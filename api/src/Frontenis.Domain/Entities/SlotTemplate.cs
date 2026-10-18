using Frontenis.Domain.Exceptions;

namespace Frontenis.Domain.Entities;

/// <summary>Plantilla semanal: un tramo horario que se repite cada semana el mismo día (p. ej. lunes de 18:00 a 20:00).</summary>
public sealed class SlotTemplate
{
    private SlotTemplate() { }

    public int Id { get; private set; }
    public DayOfWeek Day { get; private set; }
    public TimeOnly Start { get; private set; }
    public TimeOnly End { get; private set; }
    /// <summary>Si está desactivada deja de generar franjas, pero se conserva para reactivarla.</summary>
    public bool Active { get; private set; }

    /// <summary>Crea una plantilla activa.</summary>
    /// <exception cref="DomainException">La hora de fin no es posterior a la de inicio.</exception>
    public static SlotTemplate Create(DayOfWeek day, TimeOnly start, TimeOnly end)
    {
        TrainingSlot.ValidateTimes(start, end);
        return new SlotTemplate { Day = day, Start = start, End = end, Active = true };
    }

    /// <summary>Cambia día, horario y estado. Las franjas ya generadas no se modifican.</summary>
    public void Update(DayOfWeek day, TimeOnly start, TimeOnly end, bool active)
    {
        TrainingSlot.ValidateTimes(start, end);
        Day = day;
        Start = start;
        End = end;
        Active = active;
    }
}
