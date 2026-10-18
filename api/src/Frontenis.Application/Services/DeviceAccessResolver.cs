using Frontenis.Application.Exceptions;
using Frontenis.Application.Interfaces.Repositories;
using Frontenis.Application.Interfaces.Security;
using Frontenis.Application.Interfaces.Services;
using Frontenis.Application.Models;

namespace Frontenis.Application.Services;

/// <summary>Implementación de <see cref="Interfaces.Services.IDeviceAccessResolver"/>.</summary>
public sealed class DeviceAccessResolver : IDeviceAccessResolver
{
    private readonly IDeviceTokenHasher _hasher;
    private readonly IDeviceLinkRepository _links;
    private readonly IPlayerDelegateRepository _delegates;

    public DeviceAccessResolver(IDeviceTokenHasher hasher, IDeviceLinkRepository links, IPlayerDelegateRepository delegates)
    {
        _hasher = hasher;
        _links = links;
        _delegates = delegates;
    }

    public async Task<DeviceAccess?> FindAsync(string? deviceToken, CancellationToken ct = default)
    {
        if (!_hasher.IsValid(deviceToken)) return null;
        var link = await _links.GetByHashAsync(_hasher.Hash(deviceToken!), ct);
        if (link is null) return null;
        var delegates = await _delegates.ListByOwnerAsync(link.PlayerId, ct);
        return new DeviceAccess(link.PlayerId, delegates.Select(d => d.DelegatePlayerId).ToList());
    }

    public async Task<DeviceAccess> ResolveAsync(string? deviceToken, CancellationToken ct = default) =>
        await FindAsync(deviceToken, ct) ?? throw new UnauthorizedAppException("Este dispositivo no está vinculado a ningún jugador.");
}
