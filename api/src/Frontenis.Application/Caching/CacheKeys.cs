using Frontenis.Domain.Enums;
using Frontenis.Application.Models;
using Frontenis.Domain.Entities;

namespace Frontenis.Application.Caching;

/// <summary>Claves de la caché de lecturas, en un único sitio para no repetir cadenas.</summary>
public static class CacheKeys
{
    public const string CompetitionList = "competitions:list";
    public const string PlayerList = "players:list";
    public const string TrainingList = "trainings:list";

    public static string Competition(int id) => $"competitions:{id}";
    public static string CompetitionStandings(int id) => $"competitions:{id}:standings";
    public static string Training(int id) => $"trainings:{id}";
    public static string HistoryRanking(HistoryFilter filter) => $"history:ranking:{Filter(filter)}";
    public static string HistoryPlayer(int playerId, HistoryFilter filter) => $"history:player:{playerId}:{Filter(filter)}";

    private static string Filter(HistoryFilter f) =>
        $"c={string.Join(',', f.CompetitionIds.OrderBy(x => x))};t={string.Join(',', f.TrainingIds.OrderBy(x => x))};b={string.Join(',', (f.Balls ?? Array.Empty<BallType>()).Select(x => (int)x).OrderBy(x => x))}";
}
