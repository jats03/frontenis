using Frontenis.Application.Dtos;
using Frontenis.Application.Extensions;
using Frontenis.Application.Interfaces.Repositories;
using Frontenis.Application.Interfaces.Services;
using Frontenis.Application.Mapping;
using Frontenis.Domain.Entities;
using Frontenis.Domain.Exceptions;

namespace Frontenis.Application.Services;

/// <summary>Implementación de <see cref="IMatchSubmissionService"/>.</summary>
public sealed class MatchSubmissionService : IMatchSubmissionService
{
    private readonly IMatchSubmissionRepository _submissions;
    private readonly ICompetitionRepository _competitions;
    private readonly IPlayerRepository _players;
    private readonly IUnitOfWork _uow;

    public MatchSubmissionService(IMatchSubmissionRepository submissions, ICompetitionRepository competitions,
        IPlayerRepository players, IUnitOfWork uow)
    {
        _submissions = submissions;
        _competitions = competitions;
        _players = players;
        _uow = uow;
    }

    public async Task<MatchSubmissionDto> SubmitAsync(int matchId, int playerId, SubmitResultRequest request, CancellationToken ct = default)
    {
        var competition = await _competitions.GetDetailedByMatchOrThrowAsync(matchId, ct);
        var sets = (request.Sets ?? Array.Empty<SetScoreDto>()).Select(s => new SetScore(s.Home, s.Away)).ToList();
        var match = competition.EnsureSubmittable(matchId, sets);

        var existing = await _submissions.GetByMatchAsync(matchId, ct);
        if (existing is null)
        {
            existing = MatchSubmission.Create(matchId, playerId, sets);
            await _submissions.AddAsync(existing, ct);
        }
        else
        {
            existing.Replace(playerId, sets); // falla si lo envió otro jugador
        }
        await _uow.SaveChangesAsync(ct);

        var player = await _players.GetOrThrowAsync(playerId, ct);
        return ToDto(existing, competition, match, player.Name);
    }

    public async Task<IReadOnlyList<MatchSubmissionDto>> ListAsync(int competitionId, CancellationToken ct = default)
    {
        var competition = await _competitions.GetDetailedOrThrowAsync(competitionId, ct);
        var submissions = await _submissions.ListByCompetitionAsync(competitionId, ct);
        var matches = competition.Matches.ToDictionary(m => m.Id);
        var pending = submissions.Where(s => matches.TryGetValue(s.MatchId, out var m) && !m.IsPlayed).ToList();
        var names = (await _players.GetByIdsAsync(pending.Select(s => s.PlayerId).Distinct().ToList(), ct)).ToDictionary(p => p.Id, p => p.Name);
        return pending.Select(s => ToDto(s, competition, matches[s.MatchId], names.GetValueOrDefault(s.PlayerId, "—"))).ToList();
    }

    public async Task<MatchDto> ApproveAsync(int submissionId, CancellationToken ct = default)
    {
        var submission = await _submissions.GetAsync(submissionId, ct) ?? throw new NotFoundException("Envío no encontrado.");
        var competition = await _competitions.GetDetailedByMatchOrThrowAsync(submission.MatchId, ct);
        var match = competition.RecordResult(submission.MatchId, submission.Sets);
        _submissions.Remove(submission);
        await _uow.SaveChangesAsync(ct);
        return match.ToDto();
    }

    public async Task RejectAsync(int submissionId, CancellationToken ct = default)
    {
        var submission = await _submissions.GetAsync(submissionId, ct) ?? throw new NotFoundException("Envío no encontrado.");
        _submissions.Remove(submission);
        await _uow.SaveChangesAsync(ct);
    }

    private static MatchSubmissionDto ToDto(MatchSubmission s, Competition c, Match m, string playerName) => new(
        s.Id, s.MatchId, c.Id, c.Name, m.HomeEntry?.Pair.DisplayName, m.AwayEntry?.Pair.DisplayName,
        s.PlayerId, playerName, s.Sets.Select(x => new SetScoreDto(x.Home, x.Away)).ToList(), s.SubmittedAtUtc);
}
