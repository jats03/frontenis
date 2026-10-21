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
        var competition = await _competitions.GetDetailedByMatchOrThrowAsync(submission.MatchId, ct);
        await _submissions.AddRejectedAsync(RejectedSubmission.From(submission, competition.Id, DateTime.UtcNow), ct);
        _submissions.Remove(submission);
        await _uow.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<RejectedResultDto>> ListRejectedAsync(int competitionId, CancellationToken ct = default)
    {
        var competition = await _competitions.GetDetailedOrThrowAsync(competitionId, ct);
        var rejected = await _submissions.ListRejectedAsync(competitionId, ct);
        var matches = competition.Matches.ToDictionary(m => m.Id);
        var shown = rejected.Where(r => matches.TryGetValue(r.MatchId, out var m) && !m.IsPlayed).ToList();
        var names = (await _players.GetByIdsAsync(shown.Select(r => r.PlayerId).Distinct().ToList(), ct)).ToDictionary(p => p.Id, p => p.Name);
        return shown.Select(r =>
        {
            var m = matches[r.MatchId];
            return new RejectedResultDto(r.Id, r.MatchId, competitionId, m.HomeEntry?.Pair.DisplayName, m.AwayEntry?.Pair.DisplayName,
                r.PlayerId, names.GetValueOrDefault(r.PlayerId, "—"), r.Sets.Select(x => new SetScoreDto(x.Home, x.Away)).ToList(), r.RejectedAtUtc);
        }).ToList();
    }

    public async Task DeleteRejectedAsync(int rejectedId, CancellationToken ct = default)
    {
        var rejected = await _submissions.GetRejectedAsync(rejectedId, ct) ?? throw new NotFoundException("Envío rechazado no encontrado.");
        _submissions.RemoveRejected(rejected);
        await _uow.SaveChangesAsync(ct);
    }

    public async Task<MatchSubmissionDto> RestoreAsync(int rejectedId, CancellationToken ct = default)
    {
        var rejected = await _submissions.GetRejectedAsync(rejectedId, ct) ?? throw new NotFoundException("Envío rechazado no encontrado.");
        var competition = await _competitions.GetDetailedByMatchOrThrowAsync(rejected.MatchId, ct);
        var match = competition.EnsureSubmittable(rejected.MatchId, rejected.Sets);
        if (await _submissions.GetByMatchAsync(rejected.MatchId, ct) is not null)
            throw new DomainException("Este partido ya tiene otro resultado pendiente de aprobación.");

        var submission = MatchSubmission.Create(rejected.MatchId, rejected.PlayerId, rejected.Sets);
        await _submissions.AddAsync(submission, ct);
        _submissions.RemoveRejected(rejected);
        await _uow.SaveChangesAsync(ct);

        var player = await _players.GetOrThrowAsync(rejected.PlayerId, ct);
        return ToDto(submission, competition, match, player.Name);
    }

    private static MatchSubmissionDto ToDto(MatchSubmission s, Competition c, Match m, string playerName) => new(
        s.Id, s.MatchId, c.Id, c.Name, m.HomeEntry?.Pair.DisplayName, m.AwayEntry?.Pair.DisplayName,
        s.PlayerId, playerName, s.Sets.Select(x => new SetScoreDto(x.Home, x.Away)).ToList(), s.SubmittedAtUtc);
}
