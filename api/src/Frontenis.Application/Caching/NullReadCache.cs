using Frontenis.Application.Interfaces.Caching;

namespace Frontenis.Application.Caching;

/// <summary>
/// Caché desactivada: cada lectura ejecuta la consulta real contra la base de datos. Sirve para medir cuánto
/// tarda realmente (por ejemplo, tras una pausa de la BD) con la variable de entorno <c>Cache__Enabled=false</c>.
/// </summary>
public sealed class NullReadCache : IReadCache
{
    public Task<T> GetOrAddAsync<T>(string key, Func<Task<T>> factory) => factory();

    public void Clear() { }
}
