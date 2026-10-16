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

    public async Task AddAsync(DeviceLink link, CancellationToken ct = default) => await _db.DeviceLinks.AddAsync(link, ct);

    public void Remove(DeviceLink link) => _db.DeviceLinks.Remove(link);
}
