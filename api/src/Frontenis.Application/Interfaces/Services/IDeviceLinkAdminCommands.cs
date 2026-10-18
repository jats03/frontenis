using Frontenis.Application.Dtos;

namespace Frontenis.Application.Interfaces.Services;

/// <summary>Gestión de los vínculos de dispositivos por el administrador.</summary>
public interface IDeviceLinkAdminCommands
{
    /// <summary>Lista los jugadores con dispositivo vinculado.</summary>
    Task<IReadOnlyList<DeviceLinkDto>> ListAsync(CancellationToken ct = default);
    /// <summary>Libera al jugador (p. ej. si cambió de móvil) para que pueda vincularse de nuevo.</summary>
    /// <exception cref="Frontenis.Domain.Exceptions.NotFoundException">El jugador no tenía dispositivo.</exception>
    Task ReleaseAsync(int playerId, CancellationToken ct = default);
}
