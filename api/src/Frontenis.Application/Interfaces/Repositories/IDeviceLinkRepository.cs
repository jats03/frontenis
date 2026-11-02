using Frontenis.Domain.Entities;

namespace Frontenis.Application.Interfaces.Repositories;

/// <summary>Acceso a datos de vínculos entre dispositivos y jugadores.</summary>
public interface IDeviceLinkRepository
{
    /// <summary>Vínculo de un dispositivo (por su hash) o <c>null</c>.</summary>
    Task<DeviceLink?> GetByHashAsync(string deviceHash, CancellationToken ct = default);
    /// <summary>Vínculo de un jugador o <c>null</c> si no tiene dispositivo.</summary>
    Task<DeviceLink?> GetByPlayerAsync(int playerId, CancellationToken ct = default);
    /// <summary>Todos los vínculos, los más recientes primero.</summary>
    Task<IReadOnlyList<DeviceLink>> ListAsync(CancellationToken ct = default);
    /// <summary>
    /// Anota el último acceso de un vínculo solo si el anotado es anterior a <paramref name="olderThanUtc"/> (o no hay ninguno).
    /// Escribe directamente, sin pasar por la unidad de trabajo, para no vaciar la caché de lecturas por un dato así.
    /// </summary>
    Task TouchAsync(int linkId, DateTime seenUtc, DateTime olderThanUtc, CancellationToken ct = default);
    /// <summary>Añade un vínculo.</summary>
    Task AddAsync(DeviceLink link, CancellationToken ct = default);
    /// <summary>Marca el vínculo para su eliminación.</summary>
    void Remove(DeviceLink link);
}
