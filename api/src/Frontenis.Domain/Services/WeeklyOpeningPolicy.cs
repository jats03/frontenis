using Frontenis.Domain.Interfaces;

namespace Frontenis.Domain.Services;

/// <summary>
/// Abre todas las franjas de una semana (de lunes a domingo) a la vez: el último <c>openDay</c> anterior a ese lunes,
/// a la hora indicada. Con viernes a las 19:00, la semana del lunes 19 se abre el viernes 16 a las 19:00.
/// </summary>
public sealed class WeeklyOpeningPolicy : IOpeningPolicy
{
    private readonly DayOfWeek _openDay;
    private readonly TimeOnly _openTime;

    public WeeklyOpeningPolicy(DayOfWeek openDay, TimeOnly openTime)
    {
        _openDay = openDay;
        _openTime = openTime;
    }

    public DateTime OpensAtFor(DateOnly slotDate)
    {
        var day = Weeks.MondayOf(slotDate).AddDays(-1);
        while (day.DayOfWeek != _openDay) day = day.AddDays(-1);
        return day.ToDateTime(_openTime);
    }
}
