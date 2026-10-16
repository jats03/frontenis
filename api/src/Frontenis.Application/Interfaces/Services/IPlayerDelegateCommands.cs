using Frontenis.Application.Dtos;

namespace Frontenis.Application.Interfaces.Services;

/// <summary>Gestión de apadrinados por el administrador.</summary>
public interface IPlayerDelegateCommands
{
    /// <summary>Permite que el titular apunte también al apadrinado.</summary>
    /// <exception cref="Frontenis.Domain.Exceptions.NotFoundException">Algún jugador no existe.</exception>
    /// <exception cref="Frontenis.Domain.Exceptions.DomainException">El apadrinamiento ya existe o los dos jugadores son el mismo.</exception>
    Task<PlayerDelegateDto> AddAsync(AddPlayerDelegateRequest request, CancellationToken ct = default);
    /// <summary>Quita el apadrinamiento.</summary>
    Task RemoveAsync(int ownerPlayerId, int delegatePlayerId, CancellationToken ct = default);
}
