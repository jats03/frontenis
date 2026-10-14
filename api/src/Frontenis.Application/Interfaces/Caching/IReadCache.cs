namespace Frontenis.Application.Interfaces.Caching;

/// <summary>
/// Caché en memoria de lecturas públicas. No caduca por poco tiempo: se vacía entera cuando alguien escribe
/// (así las lecturas no tocan la base de datos, ni siquiera si está pausada) y tiene una caducidad larga de seguridad.
/// </summary>
public interface IReadCache
{
    /// <summary>Devuelve el valor cacheado para <paramref name="key"/> o lo calcula con <paramref name="factory"/> y lo guarda.
    /// Si varias peticiones piden la misma clave a la vez, solo una ejecuta la fábrica (las demás esperan su resultado).</summary>
    /// <typeparam name="T">Tipo del valor cacheado.</typeparam>
    /// <param name="key">Clave única de la consulta (ver <c>CacheKeys</c>).</param>
    /// <param name="factory">Función que consulta la fuente real cuando no hay valor en caché.</param>
    Task<T> GetOrAddAsync<T>(string key, Func<Task<T>> factory);
    /// <summary>Descarta todo lo cacheado (se llama tras cualquier escritura).</summary>
    void Clear();
}
