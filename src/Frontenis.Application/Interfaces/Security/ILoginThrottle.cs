namespace Frontenis.Application.Interfaces.Security;

/// <summary>Limita los intentos fallidos de inicio de sesión (por usuario y por IP).</summary>
public interface ILoginThrottle
{
    /// <summary>Tiempo que falta para poder reintentar, o null si no hay bloqueo.</summary>
    TimeSpan? LockedFor(string key);
    /// <summary>Anota un intento fallido; al llegar a <paramref name="maxFailures"/> la clave queda bloqueada durante un tiempo.</summary>
    void RegisterFailure(string key, int maxFailures);
    /// <summary>Borra los fallos de la clave (tras un inicio de sesión correcto).</summary>
    void Reset(string key);
}
