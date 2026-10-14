namespace Frontenis.Domain.Models;

/// <summary>Fila de clasificación: una pareja (Player2Id &gt; 0) o un jugador individual (Player2Id = 0).</summary>
public sealed record TrainingRow(
    int Rank, int Player1Id, int Player2Id, int Played, int Won, int Lost,
    int PointsFor, int PointsAgainst, int ClassificationPoints)
{
    public int PointsDiff => PointsFor - PointsAgainst;
}
