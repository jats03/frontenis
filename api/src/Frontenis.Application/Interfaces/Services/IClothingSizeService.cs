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
    /// <summary>Resumen de todos los jugadores para quien el administrador ha autorizado (el titular del dispositivo, no sus apadrinados).</summary>
    /// <exception cref="Frontenis.Application.Exceptions.UnauthorizedAppException">Dispositivo sin vincular.</exception>
    /// <exception cref="Frontenis.Application.Exceptions.ForbiddenAppException">El titular no tiene acceso al resumen.</exception>
    Task<IReadOnlyList<ClothingSizeDto>> ListAllForDeviceAsync(string? deviceToken, CancellationToken ct = default);
    /// <summary>Jugadores a los que el administrador ha dado acceso al resumen de tallas.</summary>
    Task<IReadOnlyList<PlayerRefDto>> ListViewersAsync(CancellationToken ct = default);
    /// <summary>Da o quita a un jugador el acceso al resumen de tallas (administrador).</summary>
    /// <exception cref="Frontenis.Domain.Exceptions.NotFoundException">El jugador no existe.</exception>
    Task<IReadOnlyList<PlayerRefDto>> SetViewerAsync(int playerId, bool allowed, CancellationToken ct = default);
}
