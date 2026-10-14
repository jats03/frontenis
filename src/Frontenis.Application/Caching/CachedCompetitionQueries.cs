using Frontenis.Application.Dtos;
using Frontenis.Application.Interfaces.Caching;
using Frontenis.Application.Interfaces.Services;

namespace Frontenis.Application.Caching;

/// <summary>Decorador: sirve las consultas de competiciones desde la caché compartida.</summary>
/// <remarks>No se propaga el token de la petición: el resultado se comparte entre todos los usuarios.</remarks>
public sealed class CachedCompetitionQueries : ICompetitionQueries
{
    private readonly ICompetitionQueries _inner;
    private readonly IReadCache _cache;

    public CachedCompetitionQueries(ICompetitionQueries inner, IReadCache cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public Task<IReadOnlyList<CompetitionSummaryDto>> ListAsync(CancellationToken ct = default) =>
        _cache.GetOrAddAsync(CacheKeys.CompetitionList, () => _inner.ListAsync(CancellationToken.None));

    public Task<CompetitionDetailDto> GetAsync(int id, CancellationToken ct = default) =>
        _cache.GetOrAddAsync(CacheKeys.Competition(id), () => _inner.GetAsync(id, CancellationToken.None));

    public Task<IReadOnlyList<GroupStandingsDto>> GetStandingsAsync(int id, CancellationToken ct = default) =>
        _cache.GetOrAddAsync(CacheKeys.CompetitionStandings(id), () => _inner.GetStandingsAsync(id, CancellationToken.None));
}
