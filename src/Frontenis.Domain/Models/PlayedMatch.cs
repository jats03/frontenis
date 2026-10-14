using Frontenis.Domain.Entities;

namespace Frontenis.Domain.Models;

/// <summary>Partido ya jugado, aplanado a jugadores (sin entidades) para calcular estadísticas.</summary>
public sealed record PlayedMatch(
    int MatchId, int CompetitionId, string CompetitionName, bool IsGroupPhase,
    int PointsPerWin, int PointsPerLoss,
    IReadOnlyList<int> HomePlayerIds, IReadOnlyList<int> AwayPlayerIds,
    IReadOnlyList<SetScore> Sets,
    bool IsTraining = false, DateTime? SortDate = null)
{
    public int HomeSets => Sets.Count(s => s.HomeWon);
    public int AwaySets => Sets.Count - HomeSets;
    public bool HomeWon => HomeSets > AwaySets;
    public int HomeTantos => Sets.Sum(s => s.Home);
    public int AwayTantos => Sets.Sum(s => s.Away);
}
