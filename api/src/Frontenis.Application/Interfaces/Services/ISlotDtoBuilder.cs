using Frontenis.Application.Dtos;
using Frontenis.Domain.Entities;

namespace Frontenis.Application.Interfaces.Services;

/// <summary>Compone los DTO de franjas: estado calculado con la hora actual y nombres de los apuntados.</summary>
public interface ISlotDtoBuilder
{
    /// <summary>Convierte una franja en su DTO.</summary>
    Task<SlotDto> BuildAsync(TrainingSlot slot, CancellationToken ct = default);
    /// <summary>Convierte varias franjas con una sola consulta de nombres, conservando el orden.</summary>
    Task<IReadOnlyList<SlotDto>> BuildManyAsync(IReadOnlyCollection<TrainingSlot> slots, CancellationToken ct = default);
}
