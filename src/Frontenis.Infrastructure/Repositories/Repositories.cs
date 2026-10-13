using Frontenis.Application.Abstractions;
using Frontenis.Domain.Entities;
using Frontenis.Domain.Exceptions;
using Frontenis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Frontenis.Infrastructure.Repositories;

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

    public Task<bool> IsInUseAsync(int playerId, CancellationToken ct = default) =>
        _db.Pairs.AnyAsync(p => p.Player1Id == playerId || p.Player2Id == playerId, ct);
}

public sealed class PairRepository : IPairRepository
{
    private readonly FrontenisDbContext _db;
    public PairRepository(FrontenisDbContext db) { _db = db; }

    public async Task AddAsync(Pair pair, CancellationToken ct = default) => await _db.Pairs.AddAsync(pair, ct);
}

public sealed class CompetitionRepository : ICompetitionRepository
{
    private readonly FrontenisDbContext _db;
    public CompetitionRepository(FrontenisDbContext db) { _db = db; }

    public async Task<IReadOnlyList<Competition>> ListAsync(CancellationToken ct = default) =>
        await _db.Competitions.Include(c => c.Entries).AsNoTracking()
            .OrderByDescending(c => c.CreatedAtUtc).ToListAsync(ct);

    public Task<Competition?> GetDetailedAsync(int id, CancellationToken ct = default) =>
        Detailed().FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<Competition?> GetDetailedByMatchIdAsync(int matchId, CancellationToken ct = default)
    {
        var competitionId = await _db.Matches.Where(m => m.Id == matchId)
            .Select(m => (int?)m.CompetitionId).FirstOrDefaultAsync(ct);
        return competitionId is null ? null : await GetDetailedAsync(competitionId.Value, ct);
    }

    public async Task AddAsync(Competition competition, CancellationToken ct = default) =>
        await _db.Competitions.AddAsync(competition, ct);

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        // Con EnableRetryOnFailure, EF Core no permite abrir una transacción manual fuera de la
        // estrategia de ejecución: toda la operación (transacción incluida) debe ir dentro de ella.
        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async token =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(token);
            await _db.Matches.Where(m => m.CompetitionId == id)
                .ExecuteUpdateAsync(s => s.SetProperty(m => m.NextMatchId, (int?)null), token);
            await _db.Matches.Where(m => m.CompetitionId == id).ExecuteDeleteAsync(token);
            var pairIds = await _db.Entries.Where(e => e.CompetitionId == id).Select(e => e.PairId).Distinct().ToListAsync(token);
            await _db.Entries.Where(e => e.CompetitionId == id).ExecuteDeleteAsync(token);
            // Las parejas de una liga rotatoria solo existen para ella: si no quedan inscripciones, se eliminan
            // (si no, bloquearían el borrado de los jugadores).
            await _db.Set<Pair>().Where(p => pairIds.Contains(p.Id) && !_db.Entries.Any(e => e.PairId == p.Id))
                .ExecuteDeleteAsync(token);
            await _db.Groups.Where(g => g.CompetitionId == id).ExecuteDeleteAsync(token);
            var deleted = await _db.Competitions.Where(c => c.Id == id).ExecuteDeleteAsync(token);
            if (deleted == 0) throw new NotFoundException("Competición no encontrada.");
            await tx.CommitAsync(token);
        }, ct);
    }

    private IQueryable<Competition> Detailed() => _db.Competitions
        .Include(c => c.Groups)
        .Include(c => c.Entries).ThenInclude(e => e.Pair).ThenInclude(p => p.Player1)
        .Include(c => c.Entries).ThenInclude(e => e.Pair).ThenInclude(p => p.Player2)
        .Include(c => c.Matches)
        .AsSplitQuery();
}

public sealed class UserRepository : IUserRepository
{
    private readonly FrontenisDbContext _db;
    public UserRepository(FrontenisDbContext db) { _db = db; }

    public Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default) =>
        _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == username, ct);

    public Task<User?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task SetPasswordHashAsync(int id, string passwordHash, CancellationToken ct = default) =>
        _db.Users.Where(u => u.Id == id).ExecuteUpdateAsync(s => s.SetProperty(u => u.PasswordHash, passwordHash), ct);
}
