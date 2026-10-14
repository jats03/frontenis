using Microsoft.EntityFrameworkCore;

namespace Frontenis.Infrastructure.Persistence;

/// <summary>Crea la BD si no existe y aplica los <see cref="SchemaScripts"/> pendientes.</summary>
public sealed class SchemaUpdater
{
    private readonly FrontenisDbContext _db;

    public SchemaUpdater(FrontenisDbContext db) { _db = db; }

    public async Task UpdateAsync(CancellationToken ct = default)
    {
        await _db.Database.EnsureCreatedAsync(ct);
        foreach (var script in SchemaScripts.All)
            await _db.Database.ExecuteSqlRawAsync(script, ct);
    }
}
