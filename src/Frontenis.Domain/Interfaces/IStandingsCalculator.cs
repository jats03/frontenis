using Frontenis.Domain.Models;

namespace Frontenis.Domain.Interfaces;

public interface IStandingsCalculator
{
    IReadOnlyList<StandingRow> Calculate(
        IReadOnlyList<int> entryIds, IEnumerable<MatchOutcome> outcomes, int pointsPerWin, int pointsPerLoss);
}
