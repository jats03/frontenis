using Frontenis.Domain.Entities;

namespace Frontenis.Application.Interfaces.Repositories;

/// <summary>Acceso a datos de apadrinados (qué jugador puede apuntar a qué otros).</summary>
public interface IPlayerDelegateRepository
{
    /// <summary>Apadrinados de un jugador titular.</summary>
    Task<IReadOnlyList<PlayerDelegate>> ListByOwnerAsync(int ownerPlayerId, CancellationToken ct = default);
    /// <summary>Todos los apadrinamientos.</summary>
    Task<IReadOnlyList<PlayerDelegate>> ListAsync(CancellationToken ct = default);
    /// <summary>Un apadrinamiento concreto o <c>null</c>.</summary>
    Task<PlayerDelegate?> GetAsync(int ownerPlayerId, int delegatePlayerId, CancellationToken ct = default);
    /// <summary>Añade un apadrinamiento.</summary>
    Task AddAsync(PlayerDelegate playerDelegate, CancellationToken ct = default);
    /// <summary>Marca el apadrinamiento para su eliminación.</summary>
    void Remove(PlayerDelegate playerDelegate);
}
