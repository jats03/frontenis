using Frontenis.Domain.Models;

namespace Frontenis.Domain.Interfaces;

/// <summary>Genera el calendario de partidos de un grupo (todos contra todos).</summary>
public interface IScheduleGenerator
{
    /// <summary>Genera el calendario round-robin; con <paramref name="doubleRound"/> se juega también la vuelta.</summary>
    IReadOnlyList<ScheduledMatch<T>> Generate<T>(IReadOnlyList<T> participants, bool doubleRound);
}
