using Frontenis.Application.Dtos;
using Frontenis.Application.Extensions;
using Frontenis.Application.Interfaces.Repositories;
using Frontenis.Application.Interfaces.Services;
using Frontenis.Application.Mapping;
using Frontenis.Domain.Entities;

namespace Frontenis.Application.Services;

/// <summary>Aplica resultados a través de la competición, que valida las reglas y avanza el cuadro.</summary>
public sealed class MatchService : IMatchService
{
    private readonly ICompetitionRepository _competitions;
    private readonly IUnitOfWork _uow;

    public MatchService(ICompetitionRepository competitions, IUnitOfWork uow)
    {
        _competitions = competitions;
        _uow = uow;
    }

    public async Task<MatchDto> SetResultAsync(int matchId, SetResultRequest request, CancellationToken ct = default)
    {
        var competition = await _competitions.GetDetailedByMatchOrThrowAsync(matchId, ct);
        var match = request.Sets is { Count: > 0 }
            ? competition.RecordResult(matchId, request.Sets.Select(s => new SetScore(s.Home, s.Away)).ToList(), request.Ball)
            : competition.RecordResult(matchId, request.HomeScore, request.AwayScore, request.Ball);
        await _uow.SaveChangesAsync(ct);
        return match.ToDto();
    }

    public async Task<MatchDto> ClearResultAsync(int matchId, CancellationToken ct = default)
    {
        var competition = await _competitions.GetDetailedByMatchOrThrowAsync(matchId, ct);
        var match = competition.ClearResult(matchId);
        await _uow.SaveChangesAsync(ct);
        return match.ToDto();
    }
}
