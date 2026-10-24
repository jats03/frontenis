using Frontenis.Application.Interfaces.Repositories;
using Frontenis.Domain.Entities;
using Frontenis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Frontenis.Infrastructure.Repositories;

/// <summary>Repositorio EF Core de entrenamientos.</summary>
public sealed class TrainingRepository : ITrainingRepository
{
    private readonly FrontenisDbContext _db;
    public TrainingRepository(FrontenisDbContext db) { _db = db; }

    public async Task<IReadOnlyList<TrainingSession>> ListAsync(CancellationToken ct = default) =>
        await _db.TrainingSessions.AsNoTracking().Include(s => s.Matches)
            .OrderByDescending(s => s.SessionDate).ThenByDescending(s => s.Id).ToListAsync(ct);

    public Task<TrainingSession?> GetAsync(int id, CancellationToken ct = default) =>
        _db.TrainingSessions.Include(s => s.Matches).FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<bool> ExistsOnAsync(DateTime date, CancellationToken ct = default) =>
        _db.TrainingSessions.AnyAsync(s => s.SessionDate == date.Date, ct);

    public Task<bool> AutoRunExistsAsync(DateTime date, CancellationToken ct = default) =>
        _db.Set<AutoTrainingRun>().AnyAsync(r => r.Date == date.Date, ct);

    public async Task AddAutoRunAsync(DateTime date, CancellationToken ct = default) =>
        await _db.Set<AutoTrainingRun>().AddAsync(new AutoTrainingRun(date), ct);

    public async Task AddAsync(TrainingSession session, CancellationToken ct = default) =>
        await _db.TrainingSessions.AddAsync(session, ct);

    public void Remove(TrainingSession session) => _db.TrainingSessions.Remove(session);
}
