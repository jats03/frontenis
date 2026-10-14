namespace Frontenis.Domain.Models;

/// <summary>Balance contra un compañero o un rival concreto.</summary>
public sealed record RelationStat(int PlayerId, int Played, int Won, int Lost, int TantosFor, int TantosAgainst)
{
    public double WinRate => Played == 0 ? 0 : Math.Round(100.0 * Won / Played, 1);
}
