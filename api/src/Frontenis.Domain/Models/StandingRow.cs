namespace Frontenis.Domain.Models;

public sealed record StandingRow(
    int EntryId, int Rank, int Played, int Won, int Lost,
    int PointsFor, int PointsAgainst, int PointsDiff, int ClassificationPoints);
