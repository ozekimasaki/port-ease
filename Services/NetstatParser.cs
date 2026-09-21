using System.Globalization;
using PortBan.Models;

namespace PortBan.Services;

internal static class NetstatParser
{
    private static readonly char[] Whitespace = [' ', '\t'];

    public static IReadOnlyList<RawEndpoint> Parse(string text)
    {
        var result = new List<RawEndpoint>();
        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length == 0)
                continue;

            var parts = line.Split(Whitespace, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 4 || !TryProtocol(parts[0], out var protocol))
                continue;

            if (!TryEndpoint(parts[1], out var address, out var port))
                continue;

            if (protocol == PortProtocol.Tcp)
            {
                if (parts.Length < 5 || !IsListening(parts[3]))
                    continue;
            }

            if (!int.TryParse(parts[^1], NumberStyles.None, CultureInfo.InvariantCulture, out var pid))
                continue;

            result.Add(new RawEndpoint(protocol, address, port, pid));
        }

        return result;
    }

    private static bool TryProtocol(string token, out PortProtocol protocol)
    {
        if (token.Equals("TCP", StringComparison.OrdinalIgnoreCase))
        {
            protocol = PortProtocol.Tcp;
            return true;
        }

        if (token.Equals("UDP", StringComparison.OrdinalIgnoreCase))
        {
            protocol = PortProtocol.Udp;
            return true;
        }

        protocol = default;
        return false;
    }

    private static bool TryEndpoint(string token, out string address, out int port)
    {
        address = "";
        port = 0;
        var colon = token.LastIndexOf(':');
        if (colon <= 0 || colon >= token.Length - 1)
            return false;

        if (!int.TryParse(token[(colon + 1)..], NumberStyles.None, CultureInfo.InvariantCulture, out port))
            return false;

        if (port is < 0 or > 65535)
            return false;

        address = token[..colon].Trim('[', ']');
        return address.Length > 0;
    }

    private static bool IsListening(string state) =>
        state.Equals("LISTENING", StringComparison.OrdinalIgnoreCase)
        || state.Equals("LISTEN", StringComparison.OrdinalIgnoreCase)
        || state.StartsWith("待機", StringComparison.Ordinal);
}
