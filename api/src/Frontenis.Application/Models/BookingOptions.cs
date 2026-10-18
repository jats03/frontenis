namespace Frontenis.Application.Models;

/// <summary>Configuración de las reservas de entrenamientos (sección <c>Bookings</c>).</summary>
public sealed class BookingOptions
{
    public const string Section = "Bookings";

    /// <summary>Mínimo recomendado de jugadores por entrenamiento; por debajo se muestra el aviso.</summary>
    public int MinPlayers { get; set; } = 4;
    /// <summary>Día de la semana en que se abren las inscripciones de la semana siguiente.</summary>
    public DayOfWeek OpenDay { get; set; } = DayOfWeek.Friday;
    /// <summary>Hora (de Madrid) a la que se abren.</summary>
    public TimeOnly OpenTime { get; set; } = new(19, 0);
}
