using System.Diagnostics;
using System.Globalization;
using System.Runtime.Versioning;
using System.Text;

namespace PortBan.Services;

internal sealed class ProcessDetails
{
    public string Name { get; set; } = "（不明）";

    public string FileDescription { get; set; } = "";

    public string CommandLine { get; set; } = "";

    public string ExecutablePath { get; set; } = "";
}

internal static class ProcessInfo
{
    public static IReadOnlyDictionary<int, ProcessDetails> Load(IEnumerable<int> pids)
    {
        var wanted = pids.Where(pid => pid > 0).ToHashSet();
        if (wanted.Count == 0)
            return new Dictionary<int, ProcessDetails>();

        if (OperatingSystem.IsWindows())
            return WindowsProcessInfo.Load(wanted);

        if (OperatingSystem.IsLinux())
            return LinuxProcessInfo.Load(wanted);

        return wanted.ToDictionary(pid => pid, _ => new ProcessDetails());
    }
}

[SupportedOSPlatform("windows")]
internal static class WindowsProcessInfo
{
    public static Dictionary<int, ProcessDetails> Load(HashSet<int> wanted)
    {
        var details = wanted.ToDictionary(pid => pid, _ => new ProcessDetails());
        try
        {
            ApplyWmi(details, wanted);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
        }

        ApplyProcessApi(details);
        foreach (var item in details.Values)
            ApplyDescription(item);

        return details;
    }

    private static void ApplyWmi(Dictionary<int, ProcessDetails> details, HashSet<int> wanted)
    {
        foreach (var chunk in wanted.Chunk(40))
        {
            var filter = string.Join(
                " OR ",
                chunk.Select(pid => "ProcessId=" + pid.ToString(CultureInfo.InvariantCulture)));
            using var searcher = new System.Management.ManagementObjectSearcher(
                "SELECT ProcessId, Name, CommandLine, ExecutablePath FROM Win32_Process WHERE " + filter);
            using var results = searcher.Get();
            foreach (System.Management.ManagementObject item in results)
            {
                using (item)
                {
                    int pid;
                    try
                    {
                        pid = Convert.ToInt32(item["ProcessId"], CultureInfo.InvariantCulture);
                    }
                    catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
                    {
                        continue;
                    }

                    Assign(details, pid, item);
                }
            }
        }
    }

    private static void Assign(
        Dictionary<int, ProcessDetails> details,
        int pid,
        System.Management.ManagementObject item)
    {
        if (!details.TryGetValue(pid, out var process))
            return;

        var path = item["ExecutablePath"] as string;
        var name = item["Name"] as string;
        var command = item["CommandLine"] as string;
        if (!string.IsNullOrWhiteSpace(path))
        {
            process.ExecutablePath = path;
            process.Name = Path.GetFileName(path);
        }
        else if (!string.IsNullOrWhiteSpace(name))
        {
            process.Name = name;
        }

        if (!string.IsNullOrWhiteSpace(command))
            process.CommandLine = command.Trim();
    }

    private static void ApplyProcessApi(Dictionary<int, ProcessDetails> details)
    {
        foreach (var process in Process.GetProcesses())
        {
            try
            {
                if (!details.TryGetValue(process.Id, out var item))
                    continue;

                if (IsUnknown(item.Name))
                    item.Name = process.ProcessName;

                if (!string.IsNullOrWhiteSpace(item.ExecutablePath))
                    continue;

                try
                {
                    var path = process.MainModule?.FileName;
                    if (!string.IsNullOrWhiteSpace(path))
                    {
                        item.ExecutablePath = path;
                        item.Name = Path.GetFileName(path);
                    }
                }
                catch (InvalidOperationException)
                {
                }
                catch (System.ComponentModel.Win32Exception)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }
            catch (InvalidOperationException)
            {
            }
            catch (System.ComponentModel.Win32Exception)
            {
            }
            finally
            {
                process.Dispose();
            }
        }
    }

    private static void ApplyDescription(ProcessDetails details)
    {
        if (string.IsNullOrWhiteSpace(details.ExecutablePath))
            return;

        try
        {
            if (!File.Exists(details.ExecutablePath))
                return;

            details.FileDescription = FileVersionInfo.GetVersionInfo(details.ExecutablePath).FileDescription?.Trim() ?? "";
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
        catch (ArgumentException)
        {
        }
    }

    private static bool IsUnknown(string name) =>
        string.IsNullOrWhiteSpace(name) || name == "（不明）";
}

[SupportedOSPlatform("linux")]
internal static class LinuxProcessInfo
{
    public static Dictionary<int, ProcessDetails> Load(HashSet<int> wanted)
    {
        var details = new Dictionary<int, ProcessDetails>(wanted.Count);
        foreach (var pid in wanted)
        {
            var item = new ProcessDetails();
            var proc = "/proc/" + pid.ToString(CultureInfo.InvariantCulture);
            try
            {
                var commandName = File.ReadAllText(proc + "/comm").Trim();
                if (commandName.Length > 0)
                    item.Name = commandName;
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }

            try
            {
                var raw = File.ReadAllBytes(proc + "/cmdline");
                var command = Encoding.UTF8.GetString(raw).Replace('\0', ' ').Trim();
                if (command.Length > 0)
                    item.CommandLine = command;
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }

            try
            {
                var path = File.ResolveLinkTarget(proc + "/exe", returnFinalTarget: false)?.FullName;
                if (!string.IsNullOrWhiteSpace(path))
                {
                    item.ExecutablePath = path;
                    item.Name = Path.GetFileName(path);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }

            details[pid] = item;
        }

        return details;
    }
}
