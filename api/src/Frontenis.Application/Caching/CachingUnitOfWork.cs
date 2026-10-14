using System.Collections.Concurrent;
using Frontenis.Application.Interfaces.Caching;
using Frontenis.Application.Interfaces.Repositories;

namespace Frontenis.Application.Caching;

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
