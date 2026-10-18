using Frontenis.Application.Interfaces.Repositories;
using Frontenis.Domain.Entities;
using Frontenis.Domain.Exceptions;
using Frontenis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Frontenis.Infrastructure.Repositories;

/// <summary>Repositorio EF Core de competiciones.</summary>
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

    public async Task DeleteOrphanPairsAsync(IReadOnlyCollection<int> pairIds, CancellationToken ct = default)
    {
        if (pairIds.Count == 0) return;
        await _db.Set<Pair>().Where(p => pairIds.Contains(p.Id) && !_db.Entries.Any(e => e.PairId == p.Id))
            .ExecuteDeleteAsync(ct);
    }

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
