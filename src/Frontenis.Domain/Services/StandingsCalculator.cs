namespace Frontenis.Domain.Services;

/// <summary>Clasificación: puntos, diferencia de tantos y tantos a favor.</summary>
public sealed class StandingsCalculator : IStandingsCalculator
{
    private sealed class Acc
    {
        public int Played, Won, Lost, For, Against;
    }

    public IReadOnlyList<StandingRow> Calculate(
        IReadOnlyList<int> entryIds, IEnumerable<MatchOutcome> outcomes, int pointsPerWin, int pointsPerLoss)
    {
        var table = entryIds.ToDictionary(id => id, _ => new Acc());

        foreach (var o in outcomes)
        {
            if (!table.TryGetValue(o.HomeId, out var home) || !table.TryGetValue(o.AwayId, out var away)) continue;
            home.Played++; away.Played++;
            home.For += o.HomeScore; home.Against += o.AwayScore;
            away.For += o.AwayScore; away.Against += o.HomeScore;
            if (o.HomeWon) { home.Won++; away.Lost++; }
            else { away.Won++; home.Lost++; }
        }

        var rows = table
            .Select(kv => new
            {
                Id = kv.Key,
                A = kv.Value,
                Pts = kv.Value.Won * pointsPerWin + kv.Value.Lost * pointsPerLoss,
                Diff = kv.Value.For - kv.Value.Against
            })
            .OrderByDescending(x => x.Pts)
            .ThenByDescending(x => x.Diff)
            .ThenByDescending(x => x.A.For)
            .ThenBy(x => x.Id)
            .ToList();

        return rows
            .Select((x, i) => new StandingRow(x.Id, i + 1, x.A.Played, x.A.Won, x.A.Lost, x.A.For, x.A.Against, x.Diff, x.Pts))
            .ToList();
    }
}
