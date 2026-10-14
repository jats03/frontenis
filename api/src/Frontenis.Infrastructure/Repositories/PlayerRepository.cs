using Frontenis.Application.Interfaces.Repositories;
using Frontenis.Domain.Entities;
using Frontenis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Frontenis.Infrastructure.Repositories;

/// <summary>Repositorio EF Core de jugadores.</summary>
public sealed class PlayerRepository : IPlayerRepository
{
    private readonly FrontenisDbContext _db;
    public PlayerRepository(FrontenisDbContext db) { _db = db; }

    public async Task<IReadOnlyList<Player>> ListAsync(CancellationToken ct = default) =>
        await _db.Players.AsNoTracking().OrderBy(p => p.Name).ToListAsync(ct);

    public async Task<IReadOnlyList<Player>> GetByIdsAsync(IReadOnlyCollection<int> ids, CancellationToken ct = default)
    {
        var found = await _db.Players.Where(p => ids.Contains(p.Id)).ToListAsync(ct);
        // conserva el orden pedido
        return ids.Select(id => found.FirstOrDefault(p => p.Id == id)).Where(p => p is not null).Select(p => p!).ToList();
    }

    public Task<Player?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _db.Players.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task AddAsync(Player player, CancellationToken ct = default) => await _db.Players.AddAsync(player, ct);

    public void Remove(Player player) => _db.Players.Remove(player);

    public async Task<bool> IsInUseAsync(int playerId, CancellationToken ct = default) =>
        await _db.Pairs.AnyAsync(p => p.Player1Id == playerId || p.Player2Id == playerId, ct)
        || await _db.TrainingMatches.AnyAsync(m =>
            m.Home1Id == playerId || m.Home2Id == playerId || m.Away1Id == playerId || m.Away2Id == playerId, ct);
}
