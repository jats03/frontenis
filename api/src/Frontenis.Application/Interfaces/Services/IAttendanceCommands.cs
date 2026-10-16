using Frontenis.Application.Dtos;

namespace Frontenis.Application.Interfaces.Services;

/// <summary>Apuntar y quitar jugadores de una franja.</summary>
public interface IAttendanceCommands
{
    /// <summary>Apunta al jugador y devuelve la franja actualizada.</summary>
    /// <exception cref="Frontenis.Domain.Exceptions.NotFoundException">La franja o el jugador no existen.</exception>
    /// <exception cref="Frontenis.Domain.Exceptions.DomainException">La franja no admite inscripciones o el jugador ya estaba apuntado.</exception>
    Task<SlotDto> SignUpAsync(int slotId, int playerId, CancellationToken ct = default);
    /// <summary>Quita al jugador y devuelve la franja actualizada.</summary>
    Task<SlotDto> WithdrawAsync(int slotId, int playerId, CancellationToken ct = default);
}
