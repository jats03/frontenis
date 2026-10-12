using Frontenis.Domain.Entities;
using Frontenis.Domain.Enums;
using Frontenis.Domain.Exceptions;

namespace Frontenis.Domain.Services;

/// <summary>
/// Genera las jornadas de un grupo de una liga con rotación. En cada jornada:
///  1. se forman parejas delantero + zaguero evitando repetir compañero (si sobran jugadores de un mismo
///     puesto, esos se emparejan entre sí, caso extremo);
///  2. las parejas se enfrentan entre sí al azar, evitando repetir rival mientras sea posible.
/// </summary>
public sealed class RotationPlanner : IRotationPlanner
{
    private const int Attempts = 120;
    private const int PartnerWeight = 1000;

    private readonly IRandomizer _randomizer;

    public RotationPlanner(IRandomizer randomizer) { _randomizer = randomizer; }

    public IReadOnlyList<PlannedRound> Plan(IReadOnlyList<Player> players, int rounds)
    {
        var n = players.Count;
        if (n < 4)
            throw new DomainException("Cada grupo necesita al menos 4 jugadores.");
        if (rounds < 1) throw new DomainException("Debe haber al menos 1 jornada.");

        // Si el grupo no es múltiplo de 4, en cada jornada descansan (n % 4) jugadores, por turnos.
        var restCount = n % 4;
        var rests = new int[n];
        var isForward = Enumerable.Range(0, n).Select(i => players[i].Position == PlayerPosition.Delantero).ToArray();

        var partners = new int[n, n];    // veces que i y j han sido compañeros
        var opponents = new int[n, n];   // veces que i y j se han enfrentado
        var result = new List<PlannedRound>();

        for (var round = 1; round <= rounds; round++)
        {
            List<(int A, int B)>? best = null;
            List<int> bestResting = new();
            var bestCost = int.MaxValue;

            for (var attempt = 0; attempt < Attempts && bestCost > 0; attempt++)
            {
                var order = Enumerable.Range(0, n).ToList();
                _randomizer.Shuffle(order);
                var resting = order.OrderBy(i => rests[i]).Take(restCount).ToList(); // OrderBy es estable: desempata el azar
                var playing = order.Where(i => !resting.Contains(i)).ToList();
                var forwards = playing.Where(i => isForward[i]).ToList();
                var backs = playing.Where(i => !isForward[i]).ToList();

                var pairs = BuildPairs(forwards, backs, partners, round);
                _randomizer.Shuffle(pairs); // pairs[0] vs pairs[1], pairs[2] vs pairs[3]…
                // Penaliza las parejas del mismo puesto (si no hay otro remedio, se aceptan).
                var cost = Cost(pairs, partners, opponents) + Math.Abs(forwards.Count - backs.Count) * 500;
                if (cost < bestCost) { bestCost = cost; best = pairs; bestResting = resting; }
            }
            foreach (var r in bestResting) rests[r]++;

            var matches = new List<PlannedMatch>();
            for (var m = 0; m + 1 < best!.Count; m += 2)
            {
                var home = best[m];
                var away = best[m + 1];
                matches.Add(new PlannedMatch(
                    (players[home.A], players[home.B]), (players[away.A], players[away.B])));
                RegisterPartners(partners, home); RegisterPartners(partners, away);
                foreach (var h in new[] { home.A, home.B })
                    foreach (var a in new[] { away.A, away.B })
                    { opponents[h, a]++; opponents[a, h]++; }
            }
            result.Add(new PlannedRound(round, matches));
        }
        return result;
    }

    /// <summary>Parejas (zaguero, delantero) minimizando compañeros repetidos; los sobrantes se emparejan entre sí.</summary>
    private List<(int A, int B)> BuildPairs(List<int> forwards, List<int> backs, int[,] partners, int roundsSoFar)
    {
        var f = forwards.ToList();
        var b = backs.ToList();
        _randomizer.Shuffle(f);
        _randomizer.Shuffle(b);

        var mixed = Math.Min(f.Count, b.Count);
        var pairs = new List<(int A, int B)>();

        if (mixed > 0)
        {
            var fm = f.Take(mixed).ToList();
            var bm = b.Take(mixed).ToList();
            var assignment = MatchWithFewestRepeats(fm, bm, partners, roundsSoFar);
            for (var i = 0; i < mixed; i++) pairs.Add((bm[assignment[i]], fm[i]));
        }

        var rest = f.Skip(mixed).Concat(b.Skip(mixed)).ToList(); // todos del mismo puesto
        for (var i = 0; i + 1 < rest.Count; i += 2) pairs.Add((rest[i], rest[i + 1]));
        return pairs;
    }

    /// <summary>
    /// Asigna a cada delantero un zaguero. Busca con retroceso el menor límite de repeticiones posible
    /// (0 = nadie repite compañero) y, a igualdad, el resultado es aleatorio.
    /// </summary>
    private int[] MatchWithFewestRepeats(List<int> forwards, List<int> backs, int[,] partners, int maxLimit)
    {
        var k = forwards.Count;
        for (var limit = 0; limit <= maxLimit; limit++)
        {
            var assignment = new int[k];
            var used = new bool[k];
            if (Search(0, limit, forwards, backs, partners, assignment, used)) return assignment;
        }
        return Enumerable.Range(0, k).ToArray(); // inalcanzable: con limit = rondas siempre hay solución
    }

    private bool Search(int i, int limit, List<int> forwards, List<int> backs, int[,] partners, int[] assignment, bool[] used)
    {
        var k = forwards.Count;
        if (i == k) return true;
        var order = Enumerable.Range(0, k).ToList();
        _randomizer.Shuffle(order);
        foreach (var j in order)
        {
            if (used[j] || partners[forwards[i], backs[j]] > limit) continue;
            used[j] = true;
            assignment[i] = j;
            if (Search(i + 1, limit, forwards, backs, partners, assignment, used)) return true;
            used[j] = false;
        }
        return false;
    }

    private static int Cost(List<(int A, int B)> pairs, int[,] partners, int[,] opponents)
    {
        var cost = 0;
        foreach (var p in pairs) cost += partners[p.A, p.B] * PartnerWeight;
        for (var m = 0; m + 1 < pairs.Count; m += 2)
            foreach (var h in new[] { pairs[m].A, pairs[m].B })
                foreach (var a in new[] { pairs[m + 1].A, pairs[m + 1].B })
                    cost += opponents[h, a];
        return cost;
    }

    private static void RegisterPartners(int[,] partners, (int A, int B) pair)
    {
        partners[pair.A, pair.B]++;
        partners[pair.B, pair.A]++;
    }
}
