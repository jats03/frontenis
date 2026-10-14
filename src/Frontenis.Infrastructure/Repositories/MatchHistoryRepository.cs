using Frontenis.Application.Interfaces.Repositories;
using Frontenis.Application.Models;
using Frontenis.Domain.Entities;
using Frontenis.Domain.Enums;
using Frontenis.Domain.Models;
using Frontenis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Frontenis.Infrastructure.Repositories;

public sealed class MatchHistoryRepository : IMatchHistoryRepository
{
    private readonly FrontenisDbContext _db;
    public MatchHistoryRepository(FrontenisDbContext db) { _db = db; }

    public async Task<IReadOnlyList<PlayedMatch>> ListPlayedAsync(HistoryFilter filter, int? playerId, CancellationToken ct = default)
    {
        var result = new List<PlayedMatch>();
        if (filter.IncludesCompetitions) result.AddRange(await CompetitionMatchesAsync(filter, playerId, ct));
        if (filter.IncludesTrainings) result.AddRange(await TrainingMatchesAsync(filter, playerId, ct));
        return result;
    }

    private async Task<List<PlayedMatch>> CompetitionMatchesAsync(HistoryFilter filter, int? playerId, CancellationToken ct)
    {
        var q = _db.Matches.AsNoTracking().Where(m =>
            m.HomeScore != null && m.AwayScore != null && m.HomeEntryId != null && m.AwayEntryId != null);
        if (filter.CompetitionIds.Count > 0)
        {
            var ids = filter.CompetitionIds.ToList();
            q = q.Where(m => ids.Contains(m.CompetitionId));
        }
        if (playerId.HasValue)
        {
            var pid = playerId.Value;
            q = q.Where(m =>
                m.HomeEntry!.Pair.Player1Id == pid || m.HomeEntry.Pair.Player2Id == pid ||
                m.AwayEntry!.Pair.Player1Id == pid || m.AwayEntry.Pair.Player2Id == pid);
        }

        var rows = await q.Select(m => new
        {
            m.Id, m.CompetitionId, m.Phase, m.HomeScore, m.AwayScore,
            m.Set1Home, m.Set1Away, m.Set2Home, m.Set2Away, m.Set3Home, m.Set3Away,
            H1 = m.HomeEntry!.Pair.Player1Id, H2 = m.HomeEntry.Pair.Player2Id,
            A1 = m.AwayEntry!.Pair.Player1Id, A2 = m.AwayEntry.Pair.Player2Id
        }).ToListAsync(ct);

        var comps = await _db.Competitions.AsNoTracking()
            .Select(c => new { c.Id, c.Name, c.PointsPerWin, c.PointsPerLoss, c.CreatedAtUtc })
            .ToDictionaryAsync(c => c.Id, ct);

        return rows.Select(r =>
        {
            var c = comps[r.CompetitionId];
            return new PlayedMatch(
                r.Id, r.CompetitionId, c.Name, r.Phase == MatchPhase.Group, c.PointsPerWin, c.PointsPerLoss,
                new[] { r.H1, r.H2 }, new[] { r.A1, r.A2 },
                SetScore.FromColumns(r.Set1Home, r.Set1Away, r.Set2Home, r.Set2Away, r.Set3Home, r.Set3Away,
                    r.HomeScore, r.AwayScore),
                false, c.CreatedAtUtc);
        }).ToList();
    }

    /// <summary>Los partidos de entrenamiento cuentan como un único set a X tantos y siempre puntúan.</summary>
    private async Task<List<PlayedMatch>> TrainingMatchesAsync(HistoryFilter filter, int? playerId, CancellationToken ct)
    {
        var q = _db.TrainingMatches.AsNoTracking().AsQueryable();
        if (filter.TrainingIds.Count > 0)
        {
            var ids = filter.TrainingIds.ToList();
            q = q.Where(m => ids.Contains(m.SessionId));
        }
        if (playerId.HasValue)
        {
            var pid = playerId.Value;
            q = q.Where(m => m.Home1Id == pid || m.Home2Id == pid || m.Away1Id == pid || m.Away2Id == pid);
        }
        var rows = await q.ToListAsync(ct);
        var sessions = await _db.TrainingSessions.AsNoTracking()
            .Select(s => new { s.Id, s.Name, s.PointsPerWin, s.PointsPerLoss }).ToDictionaryAsync(s => s.Id, ct);

        return rows.Select(m =>
        {
            var s = sessions[m.SessionId];
            return new PlayedMatch(
                m.Id, m.SessionId, s.Name, true, s.PointsPerWin, s.PointsPerLoss,
                new[] { m.Home1Id, m.Home2Id }, new[] { m.Away1Id, m.Away2Id },
                new[] { new SetScore(m.HomeScore, m.AwayScore) }, true, m.PlayedAtUtc);
        }).ToList();
    }
}
