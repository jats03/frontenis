using Frontenis.Domain.Entities;

namespace Frontenis.Application.Interfaces.Repositories;

public interface IPlayerRepository
{
    Task<IReadOnlyList<Player>> ListAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Player>> GetByIdsAsync(IReadOnlyCollection<int> ids, CancellationToken ct = default);
    Task<Player?> GetByIdAsync(int id, CancellationToken ct = default);
    Task AddAsync(Player player, CancellationToken ct = default);
    void Remove(Player player);
    Task<bool> IsInUseAsync(int playerId, CancellationToken ct = default);
}
