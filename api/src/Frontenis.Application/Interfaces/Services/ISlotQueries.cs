using Frontenis.Application.Dtos;

namespace Frontenis.Application.Interfaces.Services;

/// <summary>Consultas públicas (solo lectura) sobre las franjas de entrenamiento.</summary>
public interface ISlotQueries
{
    /// <summary>Franjas entre dos fechas (ambas incluidas, máximo 92 días). Sin fechas: esta semana y la siguiente.</summary>
    /// <exception cref="Frontenis.Domain.Exceptions.DomainException">Rango no válido.</exception>
    Task<IReadOnlyList<SlotDto>> ListAsync(DateOnly? from, DateOnly? to, CancellationToken ct = default);
}
