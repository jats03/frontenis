namespace Frontenis.Domain.Models;

public sealed record PlannedRound(int Number, IReadOnlyList<PlannedMatch> Matches);
