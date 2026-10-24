using System.Net;
using System.Net.Sockets;

namespace Frontenis.Api.RateLimiting;

/// <summary>Oculta la parte final de una IP para poder anotarla en el log sin identificar a una persona.</summary>
public static class IpAnonymizer
{
    /// <summary>IPv4: último octeto a 0 (<c>192.168.1.0</c>). IPv6: se conservan los primeros 48 bits (<c>2001:db8:1::</c>).</summary>
    public static string Anonymize(IPAddress? ip)
    {
        if (ip is null) return "desconocida";
        if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();
        var bytes = ip.GetAddressBytes();
        switch (ip.AddressFamily)
        {
            case AddressFamily.InterNetwork:
                bytes[3] = 0;
                break;
            case AddressFamily.InterNetworkV6:
                Array.Clear(bytes, 6, bytes.Length - 6);
                break;
            default:
                return "desconocida";
        }
        return new IPAddress(bytes).ToString();
    }
}
