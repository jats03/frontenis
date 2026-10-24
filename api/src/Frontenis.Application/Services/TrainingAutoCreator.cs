using Frontenis.Application.Interfaces.Repositories;
using Frontenis.Application.Interfaces.Services;
using Frontenis.Domain.Entities;
using Frontenis.Domain.Interfaces;

namespace Frontenis.Application.Services;

/// <summary>Implementación de <see cref="ITrainingAutoCreator"/>.</summary>
public sealed class TrainingAutoCreator : ITrainingAutoCreator
{
    /// <summary>Hora a partir de la cual se crea el entreno del día (Madrid).</summary>
    public static readonly TimeSpan From = TimeSpan.FromHours(8);
    /// <summary>Cada cuántas horas se revisa un martes o jueves, empezando a las 8:00 (8, 13, 18 y 23).</summary>
    public const int CheckEveryHours = 5;
    public const int PointsPerWin = 3;
    public const int PointsPerLoss = 0;

    private static readonly string[] Weekdays = { "Domingo", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado" };
    private static readonly string[] Months =
        { "enero", "febrero", "marzo", "abril", "mayo", "junio", "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre" };

    private readonly ITrainingRepository _trainings;
    private readonly IClock _clock;
    private readonly IUnitOfWork _uow;

    public TrainingAutoCreator(ITrainingRepository trainings, IClock clock, IUnitOfWork uow)
    {
        _trainings = trainings;
        _clock = clock;
        _uow = uow;
    }

    /// <summary>Días con entreno automático: martes y jueves.</summary>
    public static bool IsTrainingDay(DateTime date) => date.DayOfWeek is DayOfWeek.Tuesday or DayOfWeek.Thursday;

    /// <summary>
    /// Próxima revisión a partir de <paramref name="now"/> (hora de Madrid): los martes y jueves a las 8:00 y después cada
    /// 5 horas (13:00, 18:00 y 23:00); fuera de esos días, el siguiente martes o jueves a las 8:00.
    /// </summary>
    public static DateTime NextCheck(DateTime now)
    {
        for (var day = now.Date; day <= now.Date.AddDays(7); day = day.AddDays(1))
        {
            if (!IsTrainingDay(day)) continue;
            for (var t = From; t < TimeSpan.FromDays(1); t += TimeSpan.FromHours(CheckEveryHours))
                if (day + t > now) return day + t;
        }
        return now.Date.AddDays(1) + From;   // no se llega nunca: hay martes y jueves cada semana
    }

    /// <summary>Nombre por defecto de un entreno, el mismo que pone la web al dejar el nombre vacío: «Jueves, 22 de octubre».</summary>
    public static string DefaultName(DateTime date) =>
        $"{Weekdays[(int)date.DayOfWeek]}, {date.Day} de {Months[date.Month - 1]}";

    public async Task<bool> EnsureTodayAsync(CancellationToken ct = default)
    {
        var now = _clock.Now;
        var today = now.Date;
        if (!IsTrainingDay(today) || now.TimeOfDay < From) return false;
        if (await _trainings.AutoRunExistsAsync(today, ct)) return false;

        var created = false;
        // Si ya hay un entreno ese día (lo creó el administrador) no se duplica, pero el día se da por gestionado.
        if (!await _trainings.ExistsOnAsync(today, ct))
        {
            await _trainings.AddAsync(TrainingSession.Create(DefaultName(today), today, PointsPerWin, PointsPerLoss), ct);
            created = true;
        }
        await _trainings.AddAutoRunAsync(today, ct);
        await _uow.SaveChangesAsync(ct);
        return created;
    }
}
