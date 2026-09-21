namespace PortBan.Models;

internal readonly record struct RawEndpoint(PortProtocol Protocol, string Address, int Port, int Pid);
