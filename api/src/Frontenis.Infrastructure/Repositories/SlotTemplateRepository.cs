using Frontenis.Application.Interfaces.Repositories;
using Frontenis.Domain.Entities;
using Frontenis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Frontenis.Infrastructure.Repositories;

/// <summary>Repositorio EF Core de plantillas semanales.</summary>
public sealed class SlotTemplateRepository : ISlotTemplateRepository
{
    private readonly FrontenisDbContext _db;
    public SlotTemplateRepository(FrontenisDbContext db) { _db = db; }

    public async Task<IReadOnlyList<SlotTemplate>> ListAsync(CancellationToken ct = default)
    {
        var all = await _db.SlotTemplates.AsNoTracking().ToListAsync(ct);
        // La semana del club empieza en lunes (DayOfWeek empieza en domingo): se ordena en memoria.
        return all.OrderBy(t => ((int)t.Day + 6) % 7).ThenBy(t => t.Start).ThenBy(t => t.Id).ToList();
    }

    public Task<SlotTemplate?> GetAsync(int id, CancellationToken ct = default) =>
        _db.SlotTemplates.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task AddAsync(SlotTemplate template, CancellationToken ct = default) =>
        await _db.SlotTemplates.AddAsync(template, ct);

    public void Remove(SlotTemplate template) => _db.SlotTemplates.Remove(template);
}
