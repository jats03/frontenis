using Frontenis.Application.Dtos;

namespace Frontenis.Application.Interfaces.Services;

/// <summary>Altas, modificaciones y bajas de jugadores.</summary>
public interface IPlayerCommands
{
    /// <summary>Crea un jugador.</summary>
    Task<PlayerDto> CreateAsync(PlayerRequest request, CancellationToken ct = default);
    /// <summary>Modifica nombre, posición y grupo de un jugador.</summary>
    Task<PlayerDto> UpdateAsync(int id, PlayerRequest request, CancellationToken ct = default);
    /// <summary>Borra un jugador.</summary>
    /// <exception cref="Frontenis.Domain.Exceptions.DomainException">El jugador pertenece a una pareja de alguna competición.</exception>
    Task DeleteAsync(int id, CancellationToken ct = default);
}
