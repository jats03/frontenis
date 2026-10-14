using Frontenis.Application.Dtos;
using Frontenis.Application.Interfaces.Caching;
using Frontenis.Application.Interfaces.Services;

namespace Frontenis.Application.Caching;

/// <summary>
/// Decorador: el borrado de una competición se hace con operaciones masivas que no pasan por
/// <see cref="IUnitOfWork"/> (y por tanto no vacían la caché por sí solas), así que se vacía aquí.
/// </summary>
public sealed class CacheInvalidatingCompetitionCommands : ICompetitionCommands
{
    private readonly ICompetitionCommands _inner;
    private readonly IReadCache _cache;

    public CacheInvalidatingCompetitionCommands(ICompetitionCommands inner, IReadCache cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public Task<CompetitionSummaryDto> CreateAsync(CreateCompetitionRequest request, CancellationToken ct = default) =>
        _inner.CreateAsync(request, ct);

    public Task<CompetitionSummaryDto> RenameAsync(int id, RenameCompetitionRequest request, CancellationToken ct = default) =>
        _inner.RenameAsync(id, request, ct);

    public Task<CompetitionSummaryDto> ChangeSetsPerMatchAsync(int id, SetsPerMatchRequest request, CancellationToken ct = default) =>
        _inner.ChangeSetsPerMatchAsync(id, request, ct);

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        await _inner.DeleteAsync(id, ct);
        _cache.Clear();
    }
}
