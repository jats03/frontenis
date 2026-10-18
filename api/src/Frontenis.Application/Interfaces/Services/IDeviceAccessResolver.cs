using Frontenis.Application.Models;

namespace Frontenis.Application.Interfaces.Services;

/// <summary>Averigua a qué jugador pertenece un dispositivo y a quién puede apuntar.</summary>
public interface IDeviceAccessResolver
{
    /// <summary>Acceso del dispositivo o <c>null</c> si el identificador no es válido o no está vinculado.</summary>
    Task<DeviceAccess?> FindAsync(string? deviceToken, CancellationToken ct = default);
    /// <summary>Igual que <see cref="FindAsync"/>, pero falla si el dispositivo no está vinculado.</summary>
    /// <exception cref="Frontenis.Application.Exceptions.UnauthorizedAppException">Dispositivo sin vincular.</exception>
    Task<DeviceAccess> ResolveAsync(string? deviceToken, CancellationToken ct = default);
}
