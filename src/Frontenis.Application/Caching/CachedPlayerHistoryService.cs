using Frontenis.Application.Dtos;
using Frontenis.Application.Interfaces.Caching;
using Frontenis.Application.Interfaces.Services;
using Frontenis.Application.Models;

namespace Frontenis.Application.Caching;

public sealed class CachedPlayerHistoryService : IPlayerHistoryService
{
    private readonly IPlayerHistoryService _inner;
    private readonly IReadCache _cache;

    public CachedPlayerHistoryService(IPlayerHistoryService inner, IReadCache cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public Task<IReadOnlyList<PlayerHistoryRowDto>> RankingAsync(HistoryFilter filter, CancellationToken ct = default) =>
        _cache.GetOrAddAsync(CacheKeys.HistoryRanking(filter), () => _inner.RankingAsync(filter, CancellationToken.None));

    public Task<PlayerHistoryDto> PlayerAsync(int playerId, HistoryFilter filter, CancellationToken ct = default) =>
        _cache.GetOrAddAsync(CacheKeys.HistoryPlayer(playerId, filter), () => _inner.PlayerAsync(playerId, filter, CancellationToken.None));
}
