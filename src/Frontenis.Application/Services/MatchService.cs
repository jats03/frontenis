using Frontenis.Application.Abstractions;
using Frontenis.Application.Dtos;
using Frontenis.Domain.Exceptions;

namespace Frontenis.Application.Services;

public interface IMatchService
{
    Task<MatchDto> SetResultAsync(int matchId, SetResultRequest request, CancellationToken ct = default);
    Task<MatchDto> ClearResultAsync(int matchId, CancellationToken ct = default);
}

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
        var competition = await _competitions.GetDetailedByMatchIdAsync(matchId, ct)
            ?? throw new NotFoundException("Partido no encontrado.");
        var match = competition.RecordResult(matchId, request.HomeScore, request.AwayScore);
        await _uow.SaveChangesAsync(ct);
        return match.ToDto();
    }

    public async Task<MatchDto> ClearResultAsync(int matchId, CancellationToken ct = default)
    {
        var competition = await _competitions.GetDetailedByMatchIdAsync(matchId, ct)
            ?? throw new NotFoundException("Partido no encontrado.");
        var match = competition.ClearResult(matchId);
        await _uow.SaveChangesAsync(ct);
        return match.ToDto();
    }
}
