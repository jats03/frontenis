using System.Collections.Concurrent;

namespace Frontenis.Application.Abstractions;

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

public sealed class ReadCache : IReadCache
{
    private sealed class Entry
    {
        public required Lazy<Task<object?>> Value { get; init; }
        public required DateTime ExpiresUtc { get; init; }
        public required int Generation { get; init; }
    }

    private readonly ConcurrentDictionary<string, Entry> _entries = new();
    private readonly TimeProvider _time;
    private readonly TimeSpan _ttl;
    private readonly int _maxEntries;
    private int _generation;

    public ReadCache() : this(TimeProvider.System, TimeSpan.FromHours(12), 2000) { }
    public ReadCache(TimeProvider time, TimeSpan ttl, int maxEntries)
    {
        _time = time; _ttl = ttl; _maxEntries = maxEntries;
    }

    public void Clear()
    {
        Interlocked.Increment(ref _generation); // invalida también lo que esté calculándose ahora mismo
        _entries.Clear();
    }

    public async Task<T> GetOrAddAsync<T>(string key, Func<Task<T>> factory)
    {
        var now = _time.GetUtcNow().UtcDateTime;
        var generation = Volatile.Read(ref _generation);

        if (!_entries.TryGetValue(key, out var entry) || entry.Generation != generation || entry.ExpiresUtc <= now)
        {
            if (_entries.Count >= _maxEntries) _entries.Clear(); // tope de memoria
            var fresh = new Entry
            {
                Generation = generation,
                ExpiresUtc = now + _ttl,
                // Lazy: si varias peticiones llegan a la vez, solo una consulta la base de datos.
                Value = new Lazy<Task<object?>>(async () => await factory().ConfigureAwait(false)),
            };
            entry = _entries.AddOrUpdate(key, fresh, (_, old) =>
                old.Generation == generation && old.ExpiresUtc > now ? old : fresh);
        }

        try
        {
            var value = (T)(await entry.Value.Value.ConfigureAwait(false))!;
            // Si hubo una escritura mientras se calculaba, este resultado puede ser antiguo: no se conserva.
            if (entry.Generation != Volatile.Read(ref _generation)) Remove(key, entry);
            return value;
        }
        catch
        {
            Remove(key, entry); // los errores (p. ej. «no encontrado») nunca se cachean
            throw;
        }
    }

    private void Remove(string key, Entry entry) =>
        _entries.TryRemove(new KeyValuePair<string, Entry>(key, entry));
}

/// <summary>Cada guardado correcto vacía la caché de lecturas.</summary>
public sealed class CachingUnitOfWork : IUnitOfWork
{
    private readonly IUnitOfWork _inner;
    private readonly IReadCache _cache;
    public CachingUnitOfWork(IUnitOfWork inner, IReadCache cache) { _inner = inner; _cache = cache; }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _inner.SaveChangesAsync(ct);
        _cache.Clear();
    }
}
