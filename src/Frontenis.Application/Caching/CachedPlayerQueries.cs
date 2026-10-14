using Frontenis.Application.Dtos;
using Frontenis.Application.Interfaces.Caching;
using Frontenis.Application.Interfaces.Services;

namespace Frontenis.Application.Caching;

public sealed class CachedPlayerQueries : IPlayerQueries
{
    private readonly IPlayerQueries _inner;
    private readonly IReadCache _cache;

    public CachedPlayerQueries(IPlayerQueries inner, IReadCache cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public Task<IReadOnlyList<PlayerDto>> ListAsync(CancellationToken ct = default) =>
        _cache.GetOrAddAsync(CacheKeys.PlayerList, () => _inner.ListAsync(CancellationToken.None));
}
