using Frontenis.Domain.Entities;
using Frontenis.Domain.Models;

namespace Frontenis.Domain.Interfaces;

/// <summary>Convierte las plantillas semanales en franjas previstas para un rango de fechas.</summary>
public interface ISlotGenerator
{
    /// <summary>Franjas previstas entre <paramref name="from"/> y <paramref name="to"/> (ambas incluidas), ordenadas por fecha y hora.</summary>
    IReadOnlyList<PlannedSlot> Generate(IEnumerable<SlotTemplate> templates, DateOnly from, DateOnly to);
}
