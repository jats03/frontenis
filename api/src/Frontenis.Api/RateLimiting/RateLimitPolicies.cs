namespace Frontenis.Api.RateLimiting;

/// <summary>Nombres de las políticas de límite de peticiones (compartidos por la configuración y los endpoints).</summary>
public static class RateLimitPolicies
{
    /// <summary>Inicio de sesión y cambio de contraseña: estricto, frena la fuerza bruta.</summary>
    public const string Login = "login";

    /// <summary>Altas públicas (sin sesión), como registrar un partido de entrenamiento.</summary>
    public const string PublicWrite = "public-write";

    /// <summary>Sugerencia de partidos: lectura pública pero costosa (búsqueda combinatoria), con límite propio.</summary>
    public const string Suggestions = "suggestions";
}
