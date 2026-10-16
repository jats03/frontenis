using Frontenis.Domain.Interfaces;

namespace Frontenis.Infrastructure.Time;

/// <summary>
/// Hora local de Madrid (con cambio de horario de verano) a partir de un <see cref="TimeProvider"/>. El servidor
/// corre en UTC, pero "viernes a las 19:00" es la hora del club; al sustituir el proveedor se prueba sin esperar.
/// </summary>
public sealed class MadridClock : IClock
{
    private static readonly TimeZoneInfo Madrid = FindMadrid();
    private readonly TimeProvider _time;

    public MadridClock(TimeProvider time) { _time = time; }

    public DateTime Now => DateTime.SpecifyKind(TimeZoneInfo.ConvertTime(_time.GetUtcNow(), Madrid).DateTime, DateTimeKind.Unspecified);

    private static TimeZoneInfo FindMadrid()
    {
        // Linux/macOS usan el identificador IANA; Windows sin ICU, el suyo propio.
        foreach (var id in new[] { "Europe/Madrid", "Romance Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
        }
        throw new InvalidOperationException("No se encontró la zona horaria de Madrid; instala los datos de zonas horarias (tzdata).");
    }
}
