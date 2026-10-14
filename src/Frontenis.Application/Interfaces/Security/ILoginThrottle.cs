namespace Frontenis.Application.Interfaces.Security;

/// <summary>Limita los intentos fallidos de inicio de sesión (por usuario y por IP).</summary>
public interface ILoginThrottle
{
    /// <summary>Tiempo que falta para poder reintentar, o null si no hay bloqueo.</summary>
    TimeSpan? LockedFor(string key);
    void RegisterFailure(string key, int maxFailures);
    void Reset(string key);
}
