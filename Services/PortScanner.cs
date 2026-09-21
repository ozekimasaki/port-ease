using PortBan.Models;

namespace PortBan.Services;

internal static class PortScanner
{
    public static IReadOnlyList<PortRow> Scan()
    {
        var endpoints = ReadEndpoints()
            .GroupBy(endpoint => (endpoint.Protocol, endpoint.Address, endpoint.Port, endpoint.Pid))
            .Select(group => group.First())
            .OrderBy(endpoint => endpoint.Port)
            .ThenBy(endpoint => Label(endpoint.Protocol), StringComparer.Ordinal)
            .ThenBy(endpoint => endpoint.Address, StringComparer.Ordinal)
            .ToList();

        var processes = ProcessInfo.Load(endpoints.Select(endpoint => endpoint.Pid));
        var rows = new List<PortRow>(endpoints.Count);
        foreach (var endpoint in endpoints)
        {
            processes.TryGetValue(endpoint.Pid, out var process);
            process ??= new ProcessDetails();
            var purpose = PortCatalog.Describe(
                endpoint.Port,
                endpoint.Protocol,
                process.Name,
                process.FileDescription,
                process.CommandLine);

            rows.Add(new PortRow
            {
                Port = endpoint.Port,
                Protocol = Label(endpoint.Protocol),
                Address = AddressText.Format(endpoint.Address),
                Pid = endpoint.Pid,
                ProcessName = string.IsNullOrWhiteSpace(process.Name) ? "（不明）" : process.Name,
                Purpose = purpose,
                CommandLine = process.CommandLine,
                CanKill = endpoint.Pid > 4 && endpoint.Pid != Environment.ProcessId,
            });
        }

        return rows;
    }

    private static List<RawEndpoint> ReadEndpoints()
    {
        if (OperatingSystem.IsWindows())
            return WindowsPortScanner.Read();

        if (OperatingSystem.IsLinux())
            return LinuxPortScanner.Read();

        throw new PortScanException("この OS ではポート一覧を取得できません。");
    }

    private static string Label(PortProtocol protocol)
    {
        switch (protocol)
        {
            case PortProtocol.Tcp:
                return "TCP";
            case PortProtocol.Udp:
                return "UDP";
            default:
                return ThrowUnknown(protocol);
        }
    }

    private static string ThrowUnknown(PortProtocol protocol) =>
        throw new ArgumentOutOfRangeException(nameof(protocol), protocol, null);
}
