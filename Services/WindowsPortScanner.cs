using System.Globalization;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using PortBan.Models;

namespace PortBan.Services;

[SupportedOSPlatform("windows")]
internal static class WindowsPortScanner
{
    private const int AfInet = 2;
    private const int AfInet6 = 23;
    private const int TcpTableOwnerPidListener = 3;
    private const int UdpTableOwnerPid = 1;
    private const uint ErrorInsufficientBuffer = 122;

    public static List<RawEndpoint> Read()
    {
        try
        {
            return ReadIpHelper();
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            try
            {
                return ReadNetstat();
            }
            catch (Exception netstatError) when (netstatError is not OutOfMemoryException)
            {
                throw new PortScanException(
                    "ポート一覧を取得できませんでした。" + netstatError.Message,
                    ex);
            }
        }
    }

    private static List<RawEndpoint> ReadIpHelper()
    {
        var result = new List<RawEndpoint>();
        result.AddRange(ReadTcp(AfInet, ipv6: false));
        result.AddRange(ReadTcp(AfInet6, ipv6: true));
        result.AddRange(ReadUdp(AfInet, ipv6: false));
        result.AddRange(ReadUdp(AfInet6, ipv6: true));
        return result;
    }

    private static List<RawEndpoint> ReadTcp(int family, bool ipv6)
    {
        var size = 0;
        _ = GetExtendedTcpTable(IntPtr.Zero, ref size, true, family, TcpTableOwnerPidListener, 0);
        return ReadTable(size, (IntPtr buffer, ref int length) =>
            GetExtendedTcpTable(buffer, ref length, true, family, TcpTableOwnerPidListener, 0),
            (buffer, length) => ParseTcp(buffer, length, ipv6));
    }

    private static List<RawEndpoint> ReadUdp(int family, bool ipv6)
    {
        var size = 0;
        _ = GetExtendedUdpTable(IntPtr.Zero, ref size, true, family, UdpTableOwnerPid, 0);
        return ReadTable(size, (IntPtr buffer, ref int length) =>
            GetExtendedUdpTable(buffer, ref length, true, family, UdpTableOwnerPid, 0),
            (buffer, length) => ParseUdp(buffer, length, ipv6));
    }

    private static List<RawEndpoint> ReadTable(
        int size,
        TableQuery query,
        Func<IntPtr, int, List<RawEndpoint>> parse)
    {
        var buffer = IntPtr.Zero;
        try
        {
            for (var attempt = 0; attempt < 6; attempt++)
            {
                if (size < 4)
                    size = 4;

                buffer = Marshal.AllocHGlobal(size);
                var result = query(buffer, ref size);
                if (result == 0)
                    return parse(buffer, size);

                Marshal.FreeHGlobal(buffer);
                buffer = IntPtr.Zero;
                if (result != ErrorInsufficientBuffer)
                {
                    throw new InvalidOperationException(
                        "IP Helper が失敗しました（" + result.ToString(CultureInfo.InvariantCulture) + "）。");
                }
            }
        }
        finally
        {
            if (buffer != IntPtr.Zero)
                Marshal.FreeHGlobal(buffer);
        }

        throw new InvalidOperationException("ポート表を読めませんでした。");
    }

    private static List<RawEndpoint> ParseTcp(IntPtr buffer, int size, bool ipv6)
    {
        // MIB_TCPROW_OWNER_PID: state, addr, port, remote addr, remote port, pid。24 バイト。
        // MIB_TCP6ROW_OWNER_PID: addr16, scope, port, remote16, remote scope, remote port, state, pid。56 バイト。
        var rowSize = ipv6 ? 56 : 24;
        var count = ReadCount(buffer, size, rowSize);
        var result = new List<RawEndpoint>(count);
        for (var index = 0; index < count; index++)
        {
            var row = 4 + index * rowSize;
            string address;
            int port;
            int pid;
            if (ipv6)
            {
                address = ReadIPv6(buffer, row);
                port = ReadPort(buffer, row + 20);
                pid = Marshal.ReadInt32(buffer, row + 52);
            }
            else
            {
                address = ReadIPv4(buffer, row + 4);
                port = ReadPort(buffer, row + 8);
                pid = Marshal.ReadInt32(buffer, row + 20);
            }

            if (port == 0)
                continue;

            result.Add(new RawEndpoint(PortProtocol.Tcp, address, port, pid));
        }

        return result;
    }

    private static List<RawEndpoint> ParseUdp(IntPtr buffer, int size, bool ipv6)
    {
        // MIB_UDPROW_OWNER_PID: addr, port, pid。12 バイト。
        // MIB_UDP6ROW_OWNER_PID: addr16, scope, port, pid。28 バイト。
        var rowSize = ipv6 ? 28 : 12;
        var count = ReadCount(buffer, size, rowSize);
        var result = new List<RawEndpoint>(count);
        for (var index = 0; index < count; index++)
        {
            var row = 4 + index * rowSize;
            string address;
            int port;
            int pid;
            if (ipv6)
            {
                address = ReadIPv6(buffer, row);
                port = ReadPort(buffer, row + 20);
                pid = Marshal.ReadInt32(buffer, row + 24);
            }
            else
            {
                address = ReadIPv4(buffer, row);
                port = ReadPort(buffer, row + 4);
                pid = Marshal.ReadInt32(buffer, row + 8);
            }

            if (port == 0)
                continue;

            result.Add(new RawEndpoint(PortProtocol.Udp, address, port, pid));
        }

        return result;
    }

    private static int ReadCount(IntPtr buffer, int size, int rowSize)
    {
        if (size < 4)
            throw new InvalidOperationException("ポート表が空です。");

        var count = Marshal.ReadInt32(buffer);
        if (count < 0 || count > 100_000 || 4L + (long)count * rowSize > size)
            throw new InvalidOperationException("ポート表の件数が不正です。");

        return count;
    }

    private static string ReadIPv4(IntPtr buffer, int offset)
    {
        var value = unchecked((uint)Marshal.ReadInt32(buffer, offset));
        return new IPAddress(
        [
            (byte)(value & 0xFF),
            (byte)((value >> 8) & 0xFF),
            (byte)((value >> 16) & 0xFF),
            (byte)((value >> 24) & 0xFF),
        ]).ToString();
    }

    private static string ReadIPv6(IntPtr buffer, int offset)
    {
        var bytes = new byte[16];
        Marshal.Copy(IntPtr.Add(buffer, offset), bytes, 0, 16);
        var scope = Marshal.ReadInt32(buffer, offset + 16);
        try
        {
            var address = scope == 0 ? new IPAddress(bytes) : new IPAddress(bytes, scope);
            return address.ToString();
        }
        catch (ArgumentException)
        {
            return new IPAddress(bytes).ToString();
        }
    }

    private static int ReadPort(IntPtr buffer, int offset)
    {
        var value = Marshal.ReadInt32(buffer, offset);
        return ((value & 0xFF) << 8) | ((value >> 8) & 0xFF);
    }

    private static List<RawEndpoint> ReadNetstat()
    {
        Encoding encoding;
        try
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            encoding = Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.OEMCodePage);
        }
        catch (NotSupportedException)
        {
            encoding = Encoding.UTF8;
        }
        catch (ArgumentException)
        {
            encoding = Encoding.UTF8;
        }

        using var process = new System.Diagnostics.Process();
        process.StartInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "netstat",
            Arguments = "-ano",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = encoding,
        };

        if (!process.Start())
            throw new InvalidOperationException("netstat を起動できませんでした。");

        var output = process.StandardOutput.ReadToEnd();
        if (!process.WaitForExit(5000))
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
            }

            throw new InvalidOperationException("netstat が時間内に終わりませんでした。");
        }

        return NetstatParser.Parse(output).ToList();
    }

    private delegate uint TableQuery(IntPtr table, ref int size);

    [DllImport("iphlpapi.dll", SetLastError = true)]
    private static extern uint GetExtendedTcpTable(
        IntPtr table,
        ref int size,
        bool order,
        int addressFamily,
        int tableClass,
        uint reserved);

    [DllImport("iphlpapi.dll", SetLastError = true)]
    private static extern uint GetExtendedUdpTable(
        IntPtr table,
        ref int size,
        bool order,
        int addressFamily,
        int tableClass,
        uint reserved);
}
