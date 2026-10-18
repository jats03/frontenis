namespace Frontenis.Domain.Services;

/// <summary>Cálculos con semanas que empiezan en lunes (la semana de entrenos del club va de lunes a domingo).</summary>
public static class Weeks
{
    /// <summary>Lunes de la semana a la que pertenece <paramref name="date"/>.</summary>
    public static DateOnly MondayOf(DateOnly date) => date.AddDays(-(((int)date.DayOfWeek + 6) % 7));
}
