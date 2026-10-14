namespace Frontenis.Infrastructure.Security;

/// <summary>Configuración de los tokens JWT (clave, emisor, audiencia y duración).</summary>
public sealed class JwtOptions
{
    public const string Section = "Jwt";
    public string Issuer { get; set; } = "frontenis";
    public string Audience { get; set; } = "frontenis-clients";
    public string Key { get; set; } = string.Empty;   // mínimo 32 caracteres
    public int ExpiresMinutes { get; set; } = 240;
}
