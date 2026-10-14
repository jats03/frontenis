using Frontenis.Domain.Models;

namespace Frontenis.Domain.Services;

public static class TrainingStandings
{
    private sealed class Acc { public int P, W, L, F, A; }

    public static IReadOnlyList<TrainingRow> ByPair(IReadOnlyList<TrainingGame> games, int winPts, int lossPts) =>
        Build(games, winPts, lossPts, g => new[]
        {
            (Key(g.Home1, g.Home2), g.HomeWon, g.HomeScore, g.AwayScore),
            (Key(g.Away1, g.Away2), !g.HomeWon, g.AwayScore, g.HomeScore),
        });

    public static IReadOnlyList<TrainingRow> ByPlayer(IReadOnlyList<TrainingGame> games, int winPts, int lossPts) =>
        Build(games, winPts, lossPts, g => new[]
        {
            (Key(g.Home1, 0), g.HomeWon, g.HomeScore, g.AwayScore), (Key(g.Home2, 0), g.HomeWon, g.HomeScore, g.AwayScore),
            (Key(g.Away1, 0), !g.HomeWon, g.AwayScore, g.HomeScore), (Key(g.Away2, 0), !g.HomeWon, g.AwayScore, g.HomeScore),
        });

    public static (int, int) Key(int a, int b) => b == 0 ? (a, 0) : a < b ? (a, b) : (b, a);

    private static IReadOnlyList<TrainingRow> Build(IReadOnlyList<TrainingGame> games, int winPts, int lossPts,
        Func<TrainingGame, (( int, int) Key, bool Won, int For, int Against)[]> sides)
    {
        var acc = new Dictionary<(int, int), Acc>();
        foreach (var g in games)
            foreach (var (key, won, f, a) in sides(g))
            {
                if (!acc.TryGetValue(key, out var x)) acc[key] = x = new Acc();
                x.P++; x.F += f; x.A += a;
                if (won) x.W++; else x.L++;
            }
        var rows = acc.Select(kv => new TrainingRow(0, kv.Key.Item1, kv.Key.Item2, kv.Value.P, kv.Value.W, kv.Value.L,
                kv.Value.F, kv.Value.A, kv.Value.W * winPts + kv.Value.L * lossPts))
            .OrderByDescending(r => r.ClassificationPoints).ThenByDescending(r => r.PointsDiff)
            .ThenByDescending(r => r.PointsFor).ThenBy(r => r.Player1Id).ThenBy(r => r.Player2Id)
            .ToList();
        return rows.Select((r, i) => r with { Rank = i + 1 }).ToList();
    }
}
