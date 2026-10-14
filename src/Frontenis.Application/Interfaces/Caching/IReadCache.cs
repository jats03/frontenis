namespace Frontenis.Application.Interfaces.Caching;

/// <summary>
/// Caché en memoria de lecturas públicas. No caduca por poco tiempo: se vacía entera cuando alguien escribe
/// (así las lecturas no tocan la base de datos, ni siquiera si está pausada) y tiene una caducidad larga de seguridad.
/// </summary>
public interface IReadCache
{
    Task<T> GetOrAddAsync<T>(string key, Func<Task<T>> factory);
    /// <summary>Descarta todo lo cacheado (se llama tras cualquier escritura).</summary>
    void Clear();
}
