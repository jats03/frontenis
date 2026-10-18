using Frontenis.Application.Dtos;
using Frontenis.Application.Models;

namespace Frontenis.Application.Interfaces.Services;

/// <summary>Consultas (solo lectura) sobre las franjas de entrenamiento.</summary>
public interface ISlotQueries
{
    /// <summary>
    /// Franjas entre dos fechas (ambas incluidas, máximo 92 días). Sin fechas: esta semana y la siguiente.
    /// El público solo recibe la semana en curso y las franjas abiertas; el administrador, todas.
    /// </summary>
    /// <exception cref="Frontenis.Domain.Exceptions.DomainException">Rango no válido.</exception>
    Task<IReadOnlyList<SlotDto>> ListAsync(DateOnly? from, DateOnly? to, SlotAudience audience = SlotAudience.Public, CancellationToken ct = default);
}
