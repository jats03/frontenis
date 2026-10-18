using Frontenis.Application.Interfaces.Repositories;
using Frontenis.Domain.Entities;
using Frontenis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Frontenis.Infrastructure.Repositories;

/// <summary>Repositorio EF Core del registro de acciones.</summary>
public sealed class AuditRepository : IAuditRepository
{
    private readonly FrontenisDbContext _db;
    public AuditRepository(FrontenisDbContext db) { _db = db; }

    public async Task AddAsync(AuditEntry entry, CancellationToken ct = default)
    {
        _db.AuditEntries.Add(entry);
        await _db.SaveChangesAsync(ct);
    }

    public Task<int> PurgeOlderThanAsync(DateTime cutoffUtc, CancellationToken ct = default) =>
        _db.AuditEntries.Where(a => a.AtUtc < cutoffUtc).ExecuteDeleteAsync(ct);
}
