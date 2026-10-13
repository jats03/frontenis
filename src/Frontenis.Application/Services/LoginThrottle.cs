using System.Collections.Concurrent;
using Frontenis.Application.Abstractions;

namespace Frontenis.Application.Services;

/// <summary>
/// Bloqueo temporal tras demasiados intentos fallidos. En memoria: la API corre en una sola instancia;
/// si se reinicia, los contadores se reinician (el límite por IP del servidor sigue protegiendo).
/// </summary>
public sealed class LoginThrottle : ILoginThrottle
{
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(15);
    private const int MaxEntries = 10_000;

    private sealed class Entry { public int Count; public DateTimeOffset WindowStart; public DateTimeOffset LockedUntil; }

    private readonly ConcurrentDictionary<string, Entry> _entries = new();
    private readonly TimeProvider _time;

    public LoginThrottle(TimeProvider time) { _time = time; }

    public TimeSpan? LockedFor(string key)
    {
        if (!_entries.TryGetValue(key, out var e)) return null;
        lock (e)
        {
            var remaining = e.LockedUntil - _time.GetUtcNow();
            return remaining > TimeSpan.Zero ? remaining : null;
        }
    }

    public void RegisterFailure(string key, int maxFailures)
    {
        var now = _time.GetUtcNow();
        if (_entries.Count > MaxEntries) Purge(now);
        var e = _entries.GetOrAdd(key, _ => new Entry { WindowStart = now });
        lock (e)
        {
            if (now - e.WindowStart > Window) { e.Count = 0; e.WindowStart = now; }
            e.Count++;
            if (e.Count >= maxFailures) { e.LockedUntil = now + LockDuration; e.Count = 0; e.WindowStart = now; }
        }
    }

    public void Reset(string key) => _entries.TryRemove(key, out _);

    private void Purge(DateTimeOffset now)
    {
        foreach (var (key, e) in _entries)
            lock (e)
                if (e.LockedUntil < now && now - e.WindowStart > Window) _entries.TryRemove(key, out _);
        if (_entries.Count > MaxEntries) _entries.Clear(); // último recurso: evita crecer sin límite
    }
}
