using Frontenis.Domain.Models;

namespace Frontenis.Domain.Interfaces;

/// <summary>Clasificación individual: en <see cref="StandingRow.EntryId"/> viaja el id del jugador.</summary>
public interface IIndividualStandingsCalculator
{
    /// <summary>Calcula la clasificación individual a partir de los resultados por equipos.</summary>
    IReadOnlyList<StandingRow> Calculate(
        IReadOnlyList<int> playerIds, IEnumerable<TeamOutcome> outcomes, int pointsPerWin, int pointsPerLoss);
}
