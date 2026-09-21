namespace PortBan.Services;

public sealed class PortScanException : Exception
{
    public PortScanException(string message)
        : base(message)
    {
    }

    public PortScanException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
