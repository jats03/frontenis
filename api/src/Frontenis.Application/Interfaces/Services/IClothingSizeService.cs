using Frontenis.Application.Dtos;

namespace Frontenis.Application.Interfaces.Services;

/// <summary>Tallas de ropa: cada jugador identificado (y sus apadrinados) indica las suyas; el administrador las ve todas.</summary>
public interface IClothingSizeService
{
    /// <summary>Tallas del titular de este dispositivo y de sus apadrinados.</summary>
    /// <exception cref="Frontenis.Application.Exceptions.UnauthorizedAppException">Dispositivo sin vincular.</exception>
    Task<IReadOnlyList<ClothingSizeDto>> ListForDeviceAsync(string? deviceToken, CancellationToken ct = default);
    /// <summary>Guarda las tallas del titular o de un apadrinado de este dispositivo.</summary>
    /// <exception cref="Frontenis.Domain.Exceptions.DomainException">Talla no válida o jugador que este dispositivo no puede gestionar.</exception>
    Task<ClothingSizeDto> SaveForDeviceAsync(string? deviceToken, int playerId, SaveClothingSizeRequest request, CancellationToken ct = default);
    /// <summary>Tallas de todos los jugadores (administrador), sin el jugador comodín.</summary>
    Task<IReadOnlyList<ClothingSizeDto>> ListAllAsync(CancellationToken ct = default);
}
