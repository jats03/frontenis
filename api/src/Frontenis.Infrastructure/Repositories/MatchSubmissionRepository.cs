using Frontenis.Application.Interfaces.Repositories;
using Frontenis.Domain.Entities;
using Frontenis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Frontenis.Infrastructure.Repositories;

/// <summary>Repositorio EF Core de resultados enviados por jugadores pendientes de aprobación.</summary>
public sealed class MatchSubmissionRepository : IMatchSubmissionRepository
{
    private readonly FrontenisDbContext _db;
    public MatchSubmissionRepository(FrontenisDbContext db) { _db = db; }

    public Task<MatchSubmission?> GetByMatchAsync(int matchId, CancellationToken ct = default) =>
        _db.MatchSubmissions.FirstOrDefaultAsync(s => s.MatchId == matchId, ct);

    public Task<MatchSubmission?> GetAsync(int id, CancellationToken ct = default) =>
        _db.MatchSubmissions.FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IReadOnlyList<MatchSubmission>> ListByCompetitionAsync(int competitionId, CancellationToken ct = default) =>
        await (from s in _db.MatchSubmissions.AsNoTracking()
               join m in _db.Matches.AsNoTracking() on s.MatchId equals m.Id
               where m.CompetitionId == competitionId
               orderby s.SubmittedAtUtc, s.Id
               select s).ToListAsync(ct);

    public async Task AddAsync(MatchSubmission submission, CancellationToken ct = default) =>
        await _db.MatchSubmissions.AddAsync(submission, ct);

    public void Remove(MatchSubmission submission) => _db.MatchSubmissions.Remove(submission);
}
