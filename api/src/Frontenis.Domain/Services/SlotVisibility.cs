using Frontenis.Domain.Entities;

namespace Frontenis.Domain.Services;

/// <summary>Qué franjas puede ver el público: las de la semana en curso y las que ya tienen las inscripciones abiertas.</summary>
public static class SlotVisibility
{
    /// <param name="slot">Franja a evaluar.</param>
    /// <param name="now">Hora local actual (Madrid).</param>
    public static bool IsPublic(TrainingSlot slot, DateTime now) =>
        Weeks.MondayOf(slot.Date) == Weeks.MondayOf(DateOnly.FromDateTime(now)) || slot.IsOpen(now);
}
