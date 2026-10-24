using Frontenis.Domain.Interfaces;
using Frontenis.Domain.Models;

namespace Frontenis.Domain.Services;

/// <summary>
/// Criterio: (1) pocos partidos jugados y haber descansado el último, para que todos jueguen parecido;
/// (2) parejas lo más igualadas posible según el nivel (porcentaje de victorias suavizado + margen de tantos);
/// (3) evitar repetir compañero o el mismo cruce.
/// </summary>
public sealed class TrainingPlanner : ITrainingPlanner
{
    public IReadOnlyDictionary<int, double> Levels(IReadOnlyList<int> players, IReadOnlyList<TrainingGame> games)
    {
        var wins = players.ToDictionary(p => p, _ => 0.0);
        var played = players.ToDictionary(p => p, _ => 0.0);
        var margin = players.ToDictionary(p => p, _ => 0.0);
        foreach (var g in games)
        {
            var total = Math.Max(1, Math.Max(g.HomeScore, g.AwayScore));
            var rel = (g.HomeScore - g.AwayScore) / (double)total; // -1..1 a favor del local
            void Add(int p, bool won, double m)
            {
                if (!played.ContainsKey(p)) return;
                played[p]++; if (won) wins[p]++; margin[p] += m;
            }
            Add(g.Home1, g.HomeWon, rel); Add(g.Home2, g.HomeWon, rel);
            Add(g.Away1, !g.HomeWon, -rel); Add(g.Away2, !g.HomeWon, -rel);
        }
        // Porcentaje de victorias suavizado (con pocos partidos tira a 50) + pequeño ajuste por el margen de tantos.
        return players.ToDictionary(p => p, p =>
        {
            var smooth = (wins[p] + 1) / (played[p] + 2);
            var avgMargin = played[p] == 0 ? 0 : margin[p] / played[p];
            return Math.Clamp((smooth + 0.15 * avgMargin) * 100, 0, 100);
        });
    }

    public IReadOnlyList<TrainingSuggestion> Suggest(IReadOnlyList<int> present, IReadOnlyList<TrainingGame> games, int count,
        IReadOnlyList<TrainingGame>? history = null)
    {
        var ids = present.Distinct().OrderBy(x => x).ToList();
        if (ids.Count < 4 || count < 1) return Array.Empty<TrainingSuggestion>();

        var level = Levels(ids, history ?? games);   // el nivel sale de todo el histórico; el resto, de la sesión
        var played = ids.ToDictionary(p => p, _ => 0);
        var partners = new Dictionary<(int, int), int>();
        var matchups = new Dictionary<string, int>();
        foreach (var g in games)
        {
            foreach (var p in new[] { g.Home1, g.Home2, g.Away1, g.Away2 })
                if (played.ContainsKey(p)) played[p]++;
            foreach (var k in new[] { TrainingStandings.Key(g.Home1, g.Home2), TrainingStandings.Key(g.Away1, g.Away2) })
                partners[k] = partners.GetValueOrDefault(k) + 1;
            matchups[MatchKey(g.Home1, g.Home2, g.Away1, g.Away2)] = matchups.GetValueOrDefault(MatchKey(g.Home1, g.Home2, g.Away1, g.Away2)) + 1;
        }
        var last = games.Count == 0 ? new HashSet<int>() :
            new HashSet<int> { games[^1].Home1, games[^1].Home2, games[^1].Away1, games[^1].Away2 };
        var avgPlayed = ids.Average(p => (double)played[p]);

        var result = new List<(double Score, TrainingSuggestion S)>();
        for (var a = 0; a < ids.Count; a++)
        for (var b = a + 1; b < ids.Count; b++)
        for (var c = b + 1; c < ids.Count; c++)
        for (var d = c + 1; d < ids.Count; d++)
        {
            var q = new[] { ids[a], ids[b], ids[c], ids[d] };
            // Tres formas de repartir cuatro jugadores en dos parejas.
            foreach (var (h1, h2, a1, a2) in new[] { (q[0], q[1], q[2], q[3]), (q[0], q[2], q[1], q[3]), (q[0], q[3], q[1], q[2]) })
            {
                var home = (level[h1] + level[h2]) / 2;
                var away = (level[a1] + level[a2]) / 2;
                var gap = Math.Abs(home - away) / 100.0;                       // 0 = igualados
                // Equidad: a quien ha jugado menos que la media se le da prioridad; a quien acaba de jugar, descanso.
                var fairness = q.Sum(p => (played[p] - avgPlayed)) / 4.0;
                var rested = q.Count(p => last.Contains(p));
                var repeatPartner = partners.GetValueOrDefault(TrainingStandings.Key(h1, h2))
                                    + partners.GetValueOrDefault(TrainingStandings.Key(a1, a2));
                var repeatMatch = matchups.GetValueOrDefault(MatchKey(h1, h2, a1, a2));
                var score = gap * 4 + fairness * 1.0 + rested * 0.8 + repeatPartner * 0.4 + repeatMatch * 0.8;
                var balance = (int)Math.Round(100 * (1 - Math.Min(1, gap * 2)));
                result.Add((score, new TrainingSuggestion(h1, h2, a1, a2, Math.Round(home, 1), Math.Round(away, 1), balance,
                    Reason(balance, q.Sum(p => played[p]), rested, repeatPartner, repeatMatch))));
            }
        }
        return result.OrderBy(r => r.Score)
            .ThenBy(r => r.S.Home1).ThenBy(r => r.S.Home2).ThenBy(r => r.S.Away1).ThenBy(r => r.S.Away2)
            .Take(count).Select(r => r.S).ToList();
    }

    private static string MatchKey(int h1, int h2, int a1, int a2)
    {
        var x = TrainingStandings.Key(h1, h2); var y = TrainingStandings.Key(a1, a2);
        return x.CompareTo(y) <= 0 ? $"{x}|{y}" : $"{y}|{x}";
    }

    private static string Reason(int balance, int totalPlayed, int rested, int repeatPartner, int repeatMatch)
    {
        var parts = new List<string> { $"equilibrio {balance}%" };
        if (rested > 0) parts.Add($"{rested} jugador(es) vienen de jugar el último partido");
        if (repeatMatch > 0) parts.Add("repite un cruce ya jugado");
        else if (repeatPartner > 0) parts.Add("repite alguna pareja");
        else parts.Add("parejas nuevas");
        return string.Join(" · ", parts);
    }
}
