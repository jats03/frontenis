using Frontenis.Application.Interfaces.Repositories;
using Frontenis.Application.Models;
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

    public async Task<(IReadOnlyList<AuditEntry> Items, int Total)> SearchAsync(AuditFilter f, CancellationToken ct = default)
    {
        var q = _db.AuditEntries.AsNoTracking().AsQueryable();
        if (f.Actor is { } actor) q = q.Where(a => a.Actor == actor);
        if (!string.IsNullOrEmpty(f.Method)) q = q.Where(a => a.Method == f.Method);
        if (f.FromUtc is { } from) q = q.Where(a => a.AtUtc >= from);
        if (f.ToUtc is { } to) q = q.Where(a => a.AtUtc < to);
        if (!string.IsNullOrEmpty(f.Text))
        {
            var like = $"%{Escape(f.Text)}%";
            q = q.Where(a => EF.Functions.ILike(a.ActorName, like, "\\") || EF.Functions.ILike(a.Path, like, "\\"));
        }
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(a => a.AtUtc).ThenByDescending(a => a.Id)
            .Skip((f.Page - 1) * f.PageSize).Take(f.PageSize).ToListAsync(ct);
        return (items, total);
    }

    /// <summary>Los comodines de LIKE (% y _) y la barra se buscan tal cual, no como patrón.</summary>
    private static string Escape(string text) => text.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    public Task<int> PurgeOlderThanAsync(DateTime cutoffUtc, CancellationToken ct = default) =>
        _db.AuditEntries.Where(a => a.AtUtc < cutoffUtc).ExecuteDeleteAsync(ct);
}
