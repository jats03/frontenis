using Frontenis.Application.Interfaces.Security;

namespace Frontenis.Api.Auditing;

/// <summary>Describe el dispositivo de una petición: navegador, sistema y una huella corta del identificador (nunca el identificador).</summary>
public static class AuditDevice
{
    public const string TokenHeader = "X-Device-Token";

    /// <summary>Por ejemplo «Chrome · Android · #a1b2c3d4».</summary>
    public static string Describe(HttpContext context)
    {
        var agent = context.Request.Headers.UserAgent.ToString();
        var token = context.Request.Headers[TokenHeader].ToString();
        var hasher = context.RequestServices.GetRequiredService<IDeviceTokenHasher>();
        var fingerprint = hasher.IsValid(token) ? "#" + hasher.Hash(token)[..8] : "sin identificador";
        return AuditText.Clean($"{Browser(agent)} · {System(agent)} · {fingerprint}", Frontenis.Domain.Entities.AuditEntry.MaxDevice);
    }

    internal static string Browser(string ua) =>
        ua.Contains("Edg/") ? "Edge"
        : ua.Contains("OPR/") ? "Opera"
        : ua.Contains("Firefox/") ? "Firefox"
        : ua.Contains("Chrome/") || ua.Contains("CriOS/") ? "Chrome"
        : ua.Contains("Safari/") ? "Safari"
        : "Navegador";

    internal static string System(string ua) =>
        ua.Contains("Android") ? "Android"
        : ua.Contains("iPhone") || ua.Contains("iPad") ? "iOS"
        : ua.Contains("Windows") ? "Windows"
        : ua.Contains("Mac OS X") ? "macOS"
        : ua.Contains("Linux") ? "Linux"
        : "sistema desconocido";
}
