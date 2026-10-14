using Frontenis.Domain.Models;

namespace Frontenis.Domain.Interfaces;

/// <summary>Calcula la clasificación por parejas.</summary>
public interface IStandingsCalculator
{
    /// <summary>Calcula la clasificación por parejas: puntos por victoria/derrota y desempates.</summary>
    IReadOnlyList<StandingRow> Calculate(
        IReadOnlyList<int> entryIds, IEnumerable<MatchOutcome> outcomes, int pointsPerWin, int pointsPerLoss);
}
