using Frontenis.Application.Dtos;

namespace Frontenis.Application.Interfaces.Services;

/// <summary>Planificación por semanas: generar franjas desde las plantillas y abrir inscripciones.</summary>
public interface ISlotScheduleService
{
    /// <summary>
    /// Crea las franjas que falten para las próximas semanas. Es idempotente: si una plantilla ya generó
    /// su franja en una semana (aunque se haya movido o cancelado) no se vuelve a crear.
    /// </summary>
    /// <exception cref="Frontenis.Domain.Exceptions.DomainException">Fecha pasada o número de semanas fuera de 1-8.</exception>
    Task<GenerateSlotsResultDto> GenerateAsync(GenerateSlotsRequest request, CancellationToken ct = default);
    /// <summary>Abre ya las inscripciones de todas las franjas pendientes de la semana (lunes a domingo) de la fecha dada.</summary>
    Task<OpenWeekResultDto> OpenWeekAsync(OpenWeekRequest request, CancellationToken ct = default);
    /// <summary>
    /// Deshace «Abrir semana»: las franjas de esa semana abiertas antes de su hora habitual vuelven a quedar sin abrir.
    /// Las que ya se abren por su horario no se tocan.
    /// </summary>
    Task<CloseWeekResultDto> CloseWeekAsync(OpenWeekRequest request, CancellationToken ct = default);
}
