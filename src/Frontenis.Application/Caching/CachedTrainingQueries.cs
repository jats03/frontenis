using Frontenis.Application.Dtos;
using Frontenis.Application.Interfaces.Caching;
using Frontenis.Application.Interfaces.Services;

namespace Frontenis.Application.Caching;

public sealed class CachedTrainingQueries : ITrainingQueries
{
    private readonly ITrainingQueries _inner;
    private readonly IReadCache _cache;

    public CachedTrainingQueries(ITrainingQueries inner, IReadCache cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public Task<IReadOnlyList<TrainingSummaryDto>> ListAsync(CancellationToken ct = default) =>
        _cache.GetOrAddAsync(CacheKeys.TrainingList, () => _inner.ListAsync(CancellationToken.None));

    public Task<TrainingDetailDto> GetAsync(int id, CancellationToken ct = default) =>
        _cache.GetOrAddAsync(CacheKeys.Training(id), () => _inner.GetAsync(id, CancellationToken.None));
}
