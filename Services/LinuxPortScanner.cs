using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using PortBan.Models;

namespace PortBan.Services;

[SupportedOSPlatform("linux")]
internal static class LinuxPortScanner
{
    public static List<RawEndpoint> Read()
    {
        var sockets = new List<ProcNetParser.ParsedSocket>();
        sockets.AddRange(ReadProc("/proc/net/tcp", PortProtocol.Tcp, ipv6: false));
        sockets.AddRange(ReadProc("/proc/net/tcp6", PortProtocol.Tcp, ipv6: true));
        sockets.AddRange(ReadProc("/proc/net/udp", PortProtocol.Udp, ipv6: false));
        sockets.AddRange(ReadProc("/proc/net/udp6", PortProtocol.Udp, ipv6: true));

        var needed = sockets.Select(socket => socket.Inode).ToHashSet();
        var owners = MapInodes(needed);
        var result = new List<RawEndpoint>(sockets.Count);
        foreach (var socket in sockets)
        {
            owners.TryGetValue(socket.Inode, out var pid);
            result.Add(new RawEndpoint(socket.Protocol, socket.Address, socket.Port, pid));
        }

        return result;
    }

    private static IReadOnlyList<ProcNetParser.ParsedSocket> ReadProc(string path, PortProtocol protocol, bool ipv6)
    {
        if (!File.Exists(path))
            return [];

        try
        {
            return ProcNetParser.Parse(File.ReadAllText(path), protocol, ipv6);
        }
        catch (IOException)
        {
            return [];
        }
        catch (UnauthorizedAccessException)
        {
            return [];
        }
    }

    private static Dictionary<long, int> MapInodes(HashSet<long> needed)
    {
        var map = new Dictionary<long, int>();
        if (needed.Count == 0 || !Directory.Exists("/proc"))
            return map;

        foreach (var dir in Directory.EnumerateDirectories("/proc"))
        {
            var processName = Path.GetFileName(dir);
            if (!int.TryParse(processName, NumberStyles.None, CultureInfo.InvariantCulture, out var pid))
                continue;

            IEnumerable<string> fds;
            try
            {
                fds = Directory.EnumerateFiles(dir + "/fd");
            }
            catch (IOException)
            {
                continue;
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }

            foreach (var fd in fds)
            {
                var target = ReadLink(fd);
                if (target is null
                    || !target.StartsWith("socket:[", StringComparison.Ordinal)
                    || !target.EndsWith(']'))
                    continue;

                var inner = target.AsSpan(8, target.Length - 9);
                if (!long.TryParse(inner, NumberStyles.None, CultureInfo.InvariantCulture, out var inode))
                    continue;

                if (!needed.Contains(inode) || !map.TryAdd(inode, pid))
                    continue;

                if (map.Count == needed.Count)
                    return map;
            }
        }

        return map;
    }

    private static string? ReadLink(string path)
    {
        var buffer = new byte[96];
        var read = ReadLinkSyscall(path, buffer, (nuint)buffer.Length);
        if (read <= 0 || read >= buffer.Length)
            return null;

        return Encoding.UTF8.GetString(buffer, 0, read);
    }

    [DllImport("libc", EntryPoint = "readlink", SetLastError = true)]
    private static extern int ReadLinkSyscall(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string pathname,
        byte[] buffer,
        nuint count);
}
