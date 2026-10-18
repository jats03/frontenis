using Frontenis.Application.Dtos;

namespace Frontenis.Application.Interfaces.Services;

/// <summary>Lo que hace un jugador (sin cuenta) para identificar su dispositivo.</summary>
public interface IDeviceLinkService
{
    /// <summary>Jugador titular y apadrinados de este dispositivo, o <c>null</c> si aún no está vinculado.</summary>
    Task<DeviceMeDto?> GetMeAsync(string? deviceToken, CancellationToken ct = default);
    /// <summary>Jugadores que todavía no tienen dispositivo y por tanto pueden elegirse al vincular.</summary>
    Task<IReadOnlyList<PlayerRefDto>> ListAvailablePlayersAsync(CancellationToken ct = default);
    /// <summary>Vincula este dispositivo con un jugador (si es el mismo que ya tenía, no hace nada).</summary>
    /// <exception cref="Frontenis.Domain.Exceptions.DomainException">Dispositivo ya vinculado a otro jugador, jugador ya vinculado a otro dispositivo o identificador no válido.</exception>
    Task<DeviceMeDto> LinkAsync(string? deviceToken, int playerId, CancellationToken ct = default);
    /// <summary>Desvincula este dispositivo («no soy yo»).</summary>
    Task UnlinkAsync(string? deviceToken, CancellationToken ct = default);
}
