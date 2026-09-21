namespace PortBan.Models;

public sealed class PortRow
{
    public required int Port { get; init; }

    public required string Protocol { get; init; }

    public required string Address { get; init; }

    public required int Pid { get; init; }

    public required string ProcessName { get; init; }

    public required string Purpose { get; init; }

    public required string CommandLine { get; init; }

    public required bool CanKill { get; init; }

    public string PidText => Pid > 0 ? Pid.ToString(System.Globalization.CultureInfo.InvariantCulture) : "—";
}
