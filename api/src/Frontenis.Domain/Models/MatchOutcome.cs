namespace Frontenis.Domain.Models;

// ---------- Clasificación ----------
/// <param name="HomeSets">Sets ganados (si se indican, deciden el ganador; si no, los tantos).</param>
public sealed record MatchOutcome(int HomeId, int AwayId, int HomeScore, int AwayScore, int? HomeSets = null, int? AwaySets = null)
{
    public bool HomeWon => HomeSets.HasValue && AwaySets.HasValue ? HomeSets > AwaySets : HomeScore > AwayScore;
}
