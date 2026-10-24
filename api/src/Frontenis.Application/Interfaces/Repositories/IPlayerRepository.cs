using Frontenis.Domain.Entities;

namespace Frontenis.Application.Interfaces.Repositories;

/// <summary>Acceso a datos de jugadores.</summary>
public interface IPlayerRepository
{
    /// <summary>Lista todos los jugadores.</summary>
    Task<IReadOnlyList<Player>> ListAsync(CancellationToken ct = default);
    /// <summary>Devuelve los jugadores con esos identificadores; los que no existan simplemente no aparecen en el resultado.</summary>
    Task<IReadOnlyList<Player>> GetByIdsAsync(IReadOnlyCollection<int> ids, CancellationToken ct = default);
    /// <summary>Devuelve el jugador o <c>null</c> si no existe.</summary>
    Task<Player?> GetByIdAsync(int id, CancellationToken ct = default);
    /// <summary>Devuelve el jugador comodín «Jugador reserva», creándolo si todavía no existe.</summary>
    Task<Player> GetOrCreateReserveAsync(CancellationToken ct = default);
    /// <summary>Añade un jugador nuevo.</summary>
    Task AddAsync(Player player, CancellationToken ct = default);
    /// <summary>Marca el jugador para su eliminación.</summary>
    void Remove(Player player);
    /// <summary>Indica si el jugador forma parte de alguna pareja (en ese caso no se puede borrar).</summary>
    Task<bool> IsInUseAsync(int playerId, CancellationToken ct = default);
}
