namespace Frontenis.Domain.Models;

/// <summary>Resultado de un partido por equipos: cada lado son los ids de sus jugadores.</summary>
public sealed record TeamOutcome(
    IReadOnlyList<int> HomePlayerIds, IReadOnlyList<int> AwayPlayerIds, int HomeScore, int AwayScore,
    int? HomeSets = null, int? AwaySets = null)
{
    public bool HomeWon => HomeSets.HasValue && AwaySets.HasValue ? HomeSets > AwaySets : HomeScore > AwayScore;
}
