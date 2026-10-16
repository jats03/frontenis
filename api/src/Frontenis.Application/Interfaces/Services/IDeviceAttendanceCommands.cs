using Frontenis.Application.Dtos;

namespace Frontenis.Application.Interfaces.Services;

/// <summary>Apuntarse y quitarse desde un dispositivo vinculado: solo al jugador titular y a sus apadrinados.</summary>
public interface IDeviceAttendanceCommands
{
    /// <exception cref="Frontenis.Application.Exceptions.UnauthorizedAppException">Dispositivo sin vincular.</exception>
    /// <exception cref="Frontenis.Domain.Exceptions.DomainException">El dispositivo no puede apuntar a ese jugador o la franja no admite inscripciones.</exception>
    Task<SlotDto> SignUpAsync(string? deviceToken, int slotId, int playerId, CancellationToken ct = default);
    /// <exception cref="Frontenis.Application.Exceptions.UnauthorizedAppException">Dispositivo sin vincular.</exception>
    /// <exception cref="Frontenis.Domain.Exceptions.DomainException">El dispositivo no puede quitar a ese jugador o la franja ya empezó.</exception>
    Task<SlotDto> WithdrawAsync(string? deviceToken, int slotId, int playerId, CancellationToken ct = default);
}
