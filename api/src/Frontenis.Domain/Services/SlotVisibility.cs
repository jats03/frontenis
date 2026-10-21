using Frontenis.Domain.Entities;

namespace Frontenis.Domain.Services;

/// <summary>
/// Qué franjas puede ver el público: las de la semana en curso, las de la siguiente (aunque aún no estén abiertas,
/// para que se sepa qué hay) y las que ya tienen las inscripciones abiertas.
/// </summary>
public static class SlotVisibility
{
    /// <param name="slot">Franja a evaluar.</param>
    /// <param name="now">Hora local actual (Madrid).</param>
    public static bool IsPublic(TrainingSlot slot, DateTime now)
    {
        var thisMonday = Weeks.MondayOf(DateOnly.FromDateTime(now));
        var monday = Weeks.MondayOf(slot.Date);
        return (monday >= thisMonday && monday <= thisMonday.AddDays(7)) || slot.IsOpen(now);
    }
}
