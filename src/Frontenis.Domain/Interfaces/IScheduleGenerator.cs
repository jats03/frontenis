using Frontenis.Domain.Models;

namespace Frontenis.Domain.Interfaces;

public interface IScheduleGenerator
{
    IReadOnlyList<ScheduledMatch<T>> Generate<T>(IReadOnlyList<T> participants, bool doubleRound);
}
