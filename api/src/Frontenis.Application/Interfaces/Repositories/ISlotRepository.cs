using Frontenis.Application.Models;
using Frontenis.Domain.Entities;

namespace Frontenis.Application.Interfaces.Repositories;

/// <summary>Acceso a datos de franjas de entrenamiento y sus asistencias.</summary>
public interface ISlotRepository
{
    /// <summary>Franjas entre dos fechas (ambas incluidas) con sus apuntados, sin seguimiento, por fecha y hora.</summary>
    Task<IReadOnlyList<TrainingSlot>> ListAsync(DateOnly from, DateOnly to, CancellationToken ct = default);
    /// <summary>Igual que <see cref="ListAsync"/>, pero con seguimiento para poder modificarlas.</summary>
    Task<IReadOnlyList<TrainingSlot>> ListForUpdateAsync(DateOnly from, DateOnly to, CancellationToken ct = default);
    /// <summary>Carga la franja con sus apuntados, o <c>null</c> si no existe.</summary>
    Task<TrainingSlot?> GetAsync(int id, CancellationToken ct = default);
    /// <summary>Qué plantillas ya generaron franja en las semanas que tocan el rango dado.</summary>
    Task<IReadOnlyCollection<GeneratedSlotKey>> GeneratedKeysAsync(DateOnly from, DateOnly to, CancellationToken ct = default);
    /// <summary>Añade una franja.</summary>
    Task AddAsync(TrainingSlot slot, CancellationToken ct = default);
    /// <summary>Añade varias franjas.</summary>
    Task AddRangeAsync(IEnumerable<TrainingSlot> slots, CancellationToken ct = default);
    /// <summary>Marca la franja (y sus apuntados) para su eliminación.</summary>
    void Remove(TrainingSlot slot);
}
