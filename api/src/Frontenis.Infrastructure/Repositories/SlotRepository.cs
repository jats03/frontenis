using Frontenis.Application.Interfaces.Repositories;
using Frontenis.Application.Models;
using Frontenis.Domain.Entities;
using Frontenis.Domain.Services;
using Frontenis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Frontenis.Infrastructure.Repositories;

/// <summary>Repositorio EF Core de franjas de entrenamiento.</summary>
public sealed class SlotRepository : ISlotRepository
{
    private readonly FrontenisDbContext _db;
    public SlotRepository(FrontenisDbContext db) { _db = db; }

    public async Task<IReadOnlyList<TrainingSlot>> ListAsync(DateOnly from, DateOnly to, CancellationToken ct = default) =>
        await InRange(_db.TrainingSlots.AsNoTracking(), from, to).ToListAsync(ct);

    public async Task<IReadOnlyList<TrainingSlot>> ListForUpdateAsync(DateOnly from, DateOnly to, CancellationToken ct = default) =>
        await InRange(_db.TrainingSlots, from, to).ToListAsync(ct);

    public Task<TrainingSlot?> GetAsync(int id, CancellationToken ct = default) =>
        _db.TrainingSlots.Include(s => s.Attendees).FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IReadOnlyCollection<GeneratedSlotKey>> GeneratedKeysAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var firstDay = Weeks.MondayOf(from);
        var rows = await _db.TrainingSlots.AsNoTracking()
            .Where(s => s.TemplateId != null && s.Date >= firstDay && s.Date <= to)
            .Select(s => new { TemplateId = s.TemplateId!.Value, s.Date })
            .ToListAsync(ct);
        return rows.Select(r => new GeneratedSlotKey(r.TemplateId, Weeks.MondayOf(r.Date))).ToList();
    }

    public async Task AddAsync(TrainingSlot slot, CancellationToken ct = default) =>
        await _db.TrainingSlots.AddAsync(slot, ct);

    public Task AddRangeAsync(IEnumerable<TrainingSlot> slots, CancellationToken ct = default) =>
        _db.TrainingSlots.AddRangeAsync(slots, ct);

    public void Remove(TrainingSlot slot) => _db.TrainingSlots.Remove(slot);

    private static IQueryable<TrainingSlot> InRange(IQueryable<TrainingSlot> query, DateOnly from, DateOnly to) =>
        query.Include(s => s.Attendees)
            .Where(s => s.Date >= from && s.Date <= to)
            .OrderBy(s => s.Date).ThenBy(s => s.Start).ThenBy(s => s.Id);
}
