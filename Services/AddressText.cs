namespace PortBan.Services;

internal static class AddressText
{
    public static string Format(string address)
    {
        var text = address.Trim();
        var bare = text.Trim('[', ']');
        var zone = bare.IndexOf('%');
        var comparable = zone >= 0 ? bare[..zone] : bare;

        if (comparable is "0.0.0.0" or "::" or "*")
            return comparable + " · 全公開";

        if (comparable is "127.0.0.1" or "::1" || comparable.StartsWith("127.", StringComparison.Ordinal))
            return comparable + " · ローカルのみ";

        return bare;
    }
}
