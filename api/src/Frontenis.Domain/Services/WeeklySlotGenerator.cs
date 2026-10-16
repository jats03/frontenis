using Frontenis.Domain.Entities;
using Frontenis.Domain.Interfaces;
using Frontenis.Domain.Models;

namespace Frontenis.Domain.Services;

/// <summary>Repite cada plantilla activa en su día de la semana a lo largo del rango pedido.</summary>
public sealed class WeeklySlotGenerator : ISlotGenerator
{
    public IReadOnlyList<PlannedSlot> Generate(IEnumerable<SlotTemplate> templates, DateOnly from, DateOnly to)
    {
        var active = templates.Where(t => t.Active).ToList();
        var planned = new List<PlannedSlot>();
        for (var date = from; date <= to; date = date.AddDays(1))
            foreach (var t in active.Where(t => t.Day == date.DayOfWeek))
                planned.Add(new PlannedSlot(t.Id, date, t.Start, t.End));
        return planned.OrderBy(p => p.Date).ThenBy(p => p.Start).ToList();
    }
}
