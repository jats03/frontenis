using Frontenis.Domain.Entities;

namespace Frontenis.Domain.Services;

/// <summary>Partido ya jugado, aplanado a jugadores (sin entidades) para calcular estadísticas.</summary>
public sealed record PlayedMatch(
    int MatchId, int CompetitionId, string CompetitionName, bool IsGroupPhase,
    int PointsPerWin, int PointsPerLoss,
    IReadOnlyList<int> HomePlayerIds, IReadOnlyList<int> AwayPlayerIds,
    IReadOnlyList<SetScore> Sets)
{
    public int HomeSets => Sets.Count(s => s.HomeWon);
    public int AwaySets => Sets.Count - HomeSets;
    public bool HomeWon => HomeSets > AwaySets;
    public int HomeTantos => Sets.Sum(s => s.Home);
    public int AwayTantos => Sets.Sum(s => s.Away);
}

/// <summary>Balance contra un compañero o un rival concreto.</summary>
public sealed record RelationStat(int PlayerId, int Played, int Won, int Lost, int TantosFor, int TantosAgainst)
{
    public double WinRate => Played == 0 ? 0 : Math.Round(100.0 * Won / Played, 1);
}

public sealed record PlayerStats(
    int PlayerId, int Played, int Won, int Lost,
    int SetsFor, int SetsAgainst, int TantosFor, int TantosAgainst,
    int ClassificationPoints, int Competitions,
    IReadOnlyList<RelationStat> Partners, IReadOnlyList<RelationStat> Rivals)
{
    public int TantosDiff => TantosFor - TantosAgainst;
    public double WinRate => Played == 0 ? 0 : Math.Round(100.0 * Won / Played, 1);
}

public interface IPlayerStatsCalculator
{
    /// <summary>Estadísticas de todos los jugadores que aparecen en los partidos.</summary>
    IReadOnlyList<PlayerStats> CalculateAll(IReadOnlyList<PlayedMatch> matches);
    PlayerStats Calculate(int playerId, IReadOnlyList<PlayedMatch> matches);
}

public sealed class PlayerStatsCalculator : IPlayerStatsCalculator
{
    private sealed class Acc
    {
        public int Played, Won, Lost, SetsFor, SetsAgainst, TantosFor, TantosAgainst, Points;
        public readonly HashSet<int> Comps = new();
        public readonly Dictionary<int, int[]> Partners = new(); // played, won, lost, tf, ta
        public readonly Dictionary<int, int[]> Rivals = new();
    }

    public IReadOnlyList<PlayerStats> CalculateAll(IReadOnlyList<PlayedMatch> matches)
    {
        var acc = Accumulate(matches, null);
        return acc.Select(kv => ToStats(kv.Key, kv.Value))
            .OrderByDescending(s => s.ClassificationPoints)
            .ThenByDescending(s => s.Won)
            .ThenByDescending(s => s.TantosDiff)
            .ThenBy(s => s.PlayerId)
            .ToList();
    }

    public PlayerStats Calculate(int playerId, IReadOnlyList<PlayedMatch> matches)
    {
        var acc = Accumulate(matches, playerId);
        return acc.TryGetValue(playerId, out var a)
            ? ToStats(playerId, a)
            : new PlayerStats(playerId, 0, 0, 0, 0, 0, 0, 0, 0, 0, Array.Empty<RelationStat>(), Array.Empty<RelationStat>());
    }

    private static Dictionary<int, Acc> Accumulate(IReadOnlyList<PlayedMatch> matches, int? only)
    {
        var result = new Dictionary<int, Acc>();
        foreach (var m in matches)
        {
            if (m.Sets.Count == 0) continue;
            Side(m, m.HomePlayerIds, m.AwayPlayerIds, m.HomeWon, m.HomeSets, m.AwaySets, m.HomeTantos, m.AwayTantos, result, only);
            Side(m, m.AwayPlayerIds, m.HomePlayerIds, !m.HomeWon, m.AwaySets, m.HomeSets, m.AwayTantos, m.HomeTantos, result, only);
        }
        return result;
    }

    private static void Side(PlayedMatch m, IReadOnlyList<int> mine, IReadOnlyList<int> theirs, bool won,
        int setsFor, int setsAgainst, int tf, int ta, Dictionary<int, Acc> result, int? only)
    {
        foreach (var p in mine)
        {
            if (only.HasValue && p != only.Value) continue;
            if (!result.TryGetValue(p, out var a)) result[p] = a = new Acc();
            a.Played++;
            if (won) a.Won++; else a.Lost++;
            a.SetsFor += setsFor; a.SetsAgainst += setsAgainst;
            a.TantosFor += tf; a.TantosAgainst += ta;
            if (m.IsGroupPhase) a.Points += won ? m.PointsPerWin : m.PointsPerLoss;
            a.Comps.Add(m.CompetitionId);
            foreach (var mate in mine.Where(x => x != p)) Bump(a.Partners, mate, won, tf, ta);
            foreach (var rival in theirs) Bump(a.Rivals, rival, won, tf, ta);
        }
    }

    private static void Bump(Dictionary<int, int[]> d, int id, bool won, int tf, int ta)
    {
        if (!d.TryGetValue(id, out var r)) d[id] = r = new int[5];
        r[0]++; if (won) r[1]++; else r[2]++; r[3] += tf; r[4] += ta;
    }

    private static IReadOnlyList<RelationStat> Relations(Dictionary<int, int[]> d, bool byWins) =>
        d.Select(kv => new RelationStat(kv.Key, kv.Value[0], kv.Value[1], kv.Value[2], kv.Value[3], kv.Value[4]))
            .OrderByDescending(r => byWins ? r.Won : r.Played)
            .ThenByDescending(r => r.Played)
            .ThenBy(r => r.PlayerId)
            .ToList();

    private static PlayerStats ToStats(int id, Acc a) => new(
        id, a.Played, a.Won, a.Lost, a.SetsFor, a.SetsAgainst, a.TantosFor, a.TantosAgainst,
        a.Points, a.Comps.Count, Relations(a.Partners, false), Relations(a.Rivals, false));
}
