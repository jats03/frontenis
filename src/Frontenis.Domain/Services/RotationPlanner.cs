using Frontenis.Domain.Entities;
using Frontenis.Domain.Exceptions;
using Frontenis.Domain.Interfaces;
using Frontenis.Domain.Models;

namespace Frontenis.Domain.Services;

/// <summary>
/// Genera las jornadas de un grupo de una liga rotatoria. Todos juegan con todos (da igual el puesto: pueden
/// coincidir dos delanteros o dos zagueros). En cada jornada:
///  1. se forman parejas sin repetir compañero mientras sea posible (con n múltiplo de 4 y hasta n-1 jornadas
///     el reparto es exacto: cada jugador tiene como compañero a todos los demás una sola vez);
///  2. las parejas se enfrentan entre sí al azar, evitando repetir rival mientras sea posible;
///  3. si el grupo no es múltiplo de 4, los jugadores que sobran descansan por turnos.
/// </summary>
public sealed class RotationPlanner : IRotationPlanner
{
    private const int Attempts = 150;
    private const int SearchBudget = 20000;

    private readonly IRandomizer _randomizer;
    private int _budget;

    public RotationPlanner(IRandomizer randomizer) { _randomizer = randomizer; }

    public IReadOnlyList<PlannedRound> Plan(IReadOnlyList<Player> players, int rounds)
    {
        var n = players.Count;
        if (n < 4) throw new DomainException("Cada grupo necesita al menos 4 jugadores.");
        if (rounds < 1) throw new DomainException("Debe haber al menos 1 jornada.");

        var restCount = n % 4;
        var rests = new int[n];
        var partners = new int[n, n];    // veces que i y j han sido compañeros
        var opponents = new int[n, n];   // veces que i y j se han enfrentado
        var exact = new Queue<List<(int A, int B)>>(); // calendario exacto (circle method) si n % 4 == 0
        var result = new List<PlannedRound>();

        for (var round = 1; round <= rounds; round++)
        {
            List<(int A, int B)> basePairs;
            List<int> resting = new();

            if (restCount == 0)
            {
                if (exact.Count == 0) foreach (var r in CircleSchedule(n)) exact.Enqueue(r);
                basePairs = exact.Dequeue();
            }
            else
            {
                // Varios intentos: quién descansa (siempre entre los que menos han descansado, con un margen de 1)
                // y cómo se emparejan los demás; se queda el que menos compañeros repite.
                basePairs = new();
                var bestRepeats = int.MaxValue;
                for (var attempt = 0; attempt < 60 && bestRepeats > 0; attempt++)
                {
                    var order = Enumerable.Range(0, n).ToList();
                    _randomizer.Shuffle(order);
                    var minRests = rests.Min();
                    var slack = attempt % 2 == 0 ? 0 : 1;
                    var pool = order.Where(i => rests[i] <= minRests + slack).ToList();
                    var chosen = pool.OrderBy(i => rests[i]).Take(restCount).ToList();
                    if (chosen.Count < restCount) chosen = order.OrderBy(i => rests[i]).Take(restCount).ToList();
                    var pairs = MatchPlayers(order.Where(i => !chosen.Contains(i)).ToList(), partners);
                    var repeats = pairs.Sum(p => partners[p.A, p.B]);
                    if (repeats < bestRepeats) { bestRepeats = repeats; basePairs = pairs; resting = chosen; }
                }
            }

            // Cruces entre parejas: el mejor de varios sorteos (menos rivales repetidos).
            List<(int A, int B)>? best = null;
            var bestCost = int.MaxValue;
            for (var attempt = 0; attempt < Attempts && bestCost > 0; attempt++)
            {
                var pairs = basePairs.ToList();
                _randomizer.Shuffle(pairs); // pairs[0] vs pairs[1], pairs[2] vs pairs[3]…
                var cost = OpponentCost(pairs, opponents);
                if (cost < bestCost) { bestCost = cost; best = pairs; }
            }

            var matches = new List<PlannedMatch>();
            for (var m = 0; m + 1 < best!.Count; m += 2)
            {
                var home = best[m];
                var away = best[m + 1];
                matches.Add(new PlannedMatch(
                    (players[home.A], players[home.B]), (players[away.A], players[away.B])));
                partners[home.A, home.B]++; partners[home.B, home.A]++;
                partners[away.A, away.B]++; partners[away.B, away.A]++;
                foreach (var h in new[] { home.A, home.B })
                    foreach (var a in new[] { away.A, away.B })
                    { opponents[h, a]++; opponents[a, h]++; }
            }
            foreach (var r in resting) rests[r]++;
            result.Add(new PlannedRound(round, matches));
        }
        return result;
    }

    /// <summary>Calendario "todos con todos" (método del círculo) con jugadores y jornadas en orden aleatorio.</summary>
    private List<List<(int A, int B)>> CircleSchedule(int n)
    {
        var label = Enumerable.Range(0, n).ToList();
        _randomizer.Shuffle(label);
        var rounds = new List<List<(int A, int B)>>();
        for (var r = 0; r < n - 1; r++)
        {
            var pairs = new List<(int A, int B)> { (label[n - 1], label[r]) };
            for (var k = 1; k < n / 2; k++)
                pairs.Add((label[(r + k) % (n - 1)], label[((r - k) % (n - 1) + (n - 1)) % (n - 1)]));
            rounds.Add(pairs);
        }
        _randomizer.Shuffle(rounds);
        return rounds;
    }

    /// <summary>Empareja a los jugadores que juegan minimizando compañeros repetidos (búsqueda con retroceso).</summary>
    private List<(int A, int B)> MatchPlayers(List<int> playing, int[,] partners)
    {
        var maxSeen = 0;
        foreach (var a in playing) foreach (var b in playing) maxSeen = Math.Max(maxSeen, partners[a, b]);

        for (var limit = 0; limit <= maxSeen; limit++)
        {
            _budget = SearchBudget;
            var result = new List<(int A, int B)>();
            if (Search(playing, partners, limit, result)) return result;
        }
        var fallback = new List<(int A, int B)>();
        for (var i = 0; i + 1 < playing.Count; i += 2) fallback.Add((playing[i], playing[i + 1]));
        return fallback;
    }

    private bool Search(List<int> remaining, int[,] partners, int limit, List<(int A, int B)> acc)
    {
        if (remaining.Count == 0) return true;
        if (--_budget < 0) return false;
        var a = remaining[0];
        var candidates = remaining.Skip(1).ToList();
        _randomizer.Shuffle(candidates);
        // Primero los compañeros que menos han coincidido con a.
        foreach (var b in candidates.OrderBy(c => partners[a, c]))
        {
            if (partners[a, b] > limit) continue;
            acc.Add((a, b));
            if (Search(remaining.Where(x => x != a && x != b).ToList(), partners, limit, acc)) return true;
            acc.RemoveAt(acc.Count - 1);
            if (_budget < 0) return false;
        }
        return false;
    }

    private static int OpponentCost(List<(int A, int B)> pairs, int[,] opponents)
    {
        var cost = 0;
        for (var m = 0; m + 1 < pairs.Count; m += 2)
            foreach (var h in new[] { pairs[m].A, pairs[m].B })
                foreach (var a in new[] { pairs[m + 1].A, pairs[m + 1].B })
                    cost += opponents[h, a];
        return cost;
    }
}
