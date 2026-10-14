using Frontenis.Domain.Interfaces;
using Frontenis.Domain.Models;

namespace Frontenis.Domain.Services;

/// <summary>
/// Clasificación por jugador para ligas con rotación: cada jugador suma el resultado del partido
/// que juega con su pareja de la jornada. Desempate: puntos, diferencia de tantos y tantos a favor.
/// </summary>
public sealed class IndividualStandingsCalculator : IIndividualStandingsCalculator
{
    private sealed class Acc { public int Played, Won, Lost, For, Against; }

    public IReadOnlyList<StandingRow> Calculate(
        IReadOnlyList<int> playerIds, IEnumerable<TeamOutcome> outcomes, int pointsPerWin, int pointsPerLoss)
    {
        var table = playerIds.Distinct().ToDictionary(id => id, _ => new Acc());

        void Apply(IReadOnlyList<int> side, int scored, int conceded, bool won)
        {
            foreach (var id in side)
            {
                if (!table.TryGetValue(id, out var acc)) continue;
                acc.Played++;
                acc.For += scored;
                acc.Against += conceded;
                if (won) acc.Won++; else acc.Lost++;
            }
        }

        foreach (var o in outcomes)
        {
            var homeWon = o.HomeWon;
            Apply(o.HomePlayerIds, o.HomeScore, o.AwayScore, homeWon);
            Apply(o.AwayPlayerIds, o.AwayScore, o.HomeScore, !homeWon);
        }

        return table
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
            .Select((x, i) => new StandingRow(
                x.Id, i + 1, x.A.Played, x.A.Won, x.A.Lost, x.A.For, x.A.Against, x.Diff, x.Pts))
            .ToList();
    }
}
