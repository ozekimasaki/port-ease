using System.Globalization;
using System.Net;
using PortBan.Models;

namespace PortBan.Services;

internal static class ProcNetParser
{
    private static readonly char[] Whitespace = [' ', '\t'];

    public readonly record struct ParsedSocket(PortProtocol Protocol, string Address, int Port, long Inode);

    public static IReadOnlyList<ParsedSocket> Parse(string text, PortProtocol protocol, bool ipv6)
    {
        var result = new List<ParsedSocket>();
        foreach (var rawLine in text.Split('\n'))
        {
            var parts = rawLine.Split(Whitespace, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 10 || parts[0] == "sl")
                continue;

            var local = parts[1].Split(':');
            if (local.Length != 2)
                continue;

            if (protocol == PortProtocol.Tcp
                && !parts[3].Equals("0A", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!TryPort(local[1], out var port))
                continue;

            if (protocol == PortProtocol.Udp && RemotePort(parts[2]) != 0)
                continue;

            if (!long.TryParse(parts[9], NumberStyles.None, CultureInfo.InvariantCulture, out var inode) || inode <= 0)
                continue;

            if (!TryAddress(local[0], ipv6, out var address))
                continue;

            result.Add(new ParsedSocket(protocol, address, port, inode));
        }

        return result;
    }

    private static int RemotePort(string endpoint)
    {
        var parts = endpoint.Split(':');
        if (parts.Length != 2 || !TryPort(parts[1], out var port))
            return -1;

        return port;
    }

    private static bool TryPort(string hex, out int port)
    {
        port = 0;
        if (!int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out port))
            return false;

        return port is >= 0 and <= 65535;
    }

    private static bool TryAddress(string hex, bool ipv6, out string address)
    {
        address = "";
        if (!ipv6)
        {
            if (hex.Length != 8)
                return false;

            var value = Convert.ToUInt32(hex, 16);
            address = new IPAddress(
            [
                (byte)(value & 0xFF),
                (byte)((value >> 8) & 0xFF),
                (byte)((value >> 16) & 0xFF),
                (byte)((value >> 24) & 0xFF),
            ]).ToString();
            return true;
        }

        if (hex.Length != 32)
            return false;

        var bytes = new byte[16];
        for (var word = 0; word < 4; word++)
        {
            var raw = Convert.FromHexString(hex.Substring(word * 8, 8));
            bytes[word * 4] = raw[3];
            bytes[word * 4 + 1] = raw[2];
            bytes[word * 4 + 2] = raw[1];
            bytes[word * 4 + 3] = raw[0];
        }

        address = new IPAddress(bytes).ToString();
        return true;
    }
}
