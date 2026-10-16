using Frontenis.Application.Interfaces.Repositories;
using Frontenis.Domain.Entities;
using Frontenis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Frontenis.Infrastructure.Repositories;

/// <summary>Repositorio EF Core de apadrinados.</summary>
public sealed class PlayerDelegateRepository : IPlayerDelegateRepository
{
    private readonly FrontenisDbContext _db;
    public PlayerDelegateRepository(FrontenisDbContext db) { _db = db; }

    public async Task<IReadOnlyList<PlayerDelegate>> ListByOwnerAsync(int ownerPlayerId, CancellationToken ct = default) =>
        await _db.PlayerDelegates.AsNoTracking().Where(d => d.OwnerPlayerId == ownerPlayerId).OrderBy(d => d.Id).ToListAsync(ct);

    public async Task<IReadOnlyList<PlayerDelegate>> ListAsync(CancellationToken ct = default) =>
        await _db.PlayerDelegates.AsNoTracking().OrderBy(d => d.Id).ToListAsync(ct);

    public Task<PlayerDelegate?> GetAsync(int ownerPlayerId, int delegatePlayerId, CancellationToken ct = default) =>
        _db.PlayerDelegates.FirstOrDefaultAsync(d => d.OwnerPlayerId == ownerPlayerId && d.DelegatePlayerId == delegatePlayerId, ct);

    public async Task AddAsync(PlayerDelegate playerDelegate, CancellationToken ct = default) =>
        await _db.PlayerDelegates.AddAsync(playerDelegate, ct);

    public void Remove(PlayerDelegate playerDelegate) => _db.PlayerDelegates.Remove(playerDelegate);
}
