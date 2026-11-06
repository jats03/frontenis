using Frontenis.Application.Dtos;

namespace Frontenis.Application.Interfaces.Services;

/// <summary>Avatar de los jugadores: lo cambia el titular del dispositivo (o un apadrinado suyo).</summary>
public interface IAvatarService
{
    /// <summary>Cambia el avatar del titular del dispositivo o de uno de sus apadrinados (vacío = el de siempre).</summary>
    /// <exception cref="Frontenis.Application.Exceptions.UnauthorizedAppException">Dispositivo sin vincular.</exception>
    /// <exception cref="Frontenis.Domain.Exceptions.DomainException">Avatar no válido o jugador que este dispositivo no puede gestionar.</exception>
    Task<AvatarDto> SetForDeviceAsync(string? deviceToken, int playerId, int? avatar, CancellationToken ct = default);
}
