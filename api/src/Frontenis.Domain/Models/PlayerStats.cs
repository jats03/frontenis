namespace Frontenis.Domain.Models;

public sealed record PlayerStats(
    int PlayerId, int Played, int Won, int Lost,
    int SetsFor, int SetsAgainst, int TantosFor, int TantosAgainst,
    int ClassificationPoints, int Competitions,
    IReadOnlyList<RelationStat> Partners, IReadOnlyList<RelationStat> Rivals)
{
    public int TantosDiff => TantosFor - TantosAgainst;
    public double WinRate => Played == 0 ? 0 : Math.Round(100.0 * Won / Played, 1);
}
