using Frontenis.Application.Interfaces.Repositories;
using Frontenis.Domain.Entities;
using Frontenis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Frontenis.Infrastructure.Repositories;

/// <summary>Repositorio EF Core de vínculos entre dispositivos y jugadores.</summary>
public sealed class DeviceLinkRepository : IDeviceLinkRepository
{
    private readonly FrontenisDbContext _db;
    public DeviceLinkRepository(FrontenisDbContext db) { _db = db; }

    public Task<DeviceLink?> GetByHashAsync(string deviceHash, CancellationToken ct = default) =>
        _db.DeviceLinks.FirstOrDefaultAsync(l => l.DeviceHash == deviceHash, ct);

    public Task<DeviceLink?> GetByPlayerAsync(int playerId, CancellationToken ct = default) =>
        _db.DeviceLinks.FirstOrDefaultAsync(l => l.PlayerId == playerId, ct);

    public async Task<IReadOnlyList<DeviceLink>> ListAsync(CancellationToken ct = default) =>
        await _db.DeviceLinks.AsNoTracking().OrderByDescending(l => l.LinkedAtUtc).ThenBy(l => l.Id).ToListAsync(ct);

    public async Task TouchAsync(int linkId, DateTime seenUtc, DateTime olderThanUtc, CancellationToken ct = default)
    {
        // Un UPDATE directo y condicional: una sola escritura por hora y dispositivo, aunque lleguen varias aperturas a la vez.
        var seen = DateTime.SpecifyKind(seenUtc, DateTimeKind.Utc);
        var limit = DateTime.SpecifyKind(olderThanUtc, DateTimeKind.Utc);
        await _db.DeviceLinks
            .Where(l => l.Id == linkId && (l.LastSeenUtc == null || l.LastSeenUtc < limit))
            .ExecuteUpdateAsync(s => s.SetProperty(l => l.LastSeenUtc, seen), ct);
    }

    public async Task AddAsync(DeviceLink link, CancellationToken ct = default) => await _db.DeviceLinks.AddAsync(link, ct);

    public void Remove(DeviceLink link) => _db.DeviceLinks.Remove(link);
}
