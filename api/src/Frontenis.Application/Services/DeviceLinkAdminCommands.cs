using Frontenis.Application.Dtos;
using Frontenis.Application.Interfaces.Repositories;
using Frontenis.Application.Interfaces.Services;
using Frontenis.Domain.Exceptions;

namespace Frontenis.Application.Services;

/// <summary>Implementación de <see cref="Interfaces.Services.IDeviceLinkAdminCommands"/>.</summary>
public sealed class DeviceLinkAdminCommands : IDeviceLinkAdminCommands
{
    private readonly IDeviceLinkRepository _links;
    private readonly IPlayerRepository _players;
    private readonly IUnitOfWork _uow;

    public DeviceLinkAdminCommands(IDeviceLinkRepository links, IPlayerRepository players, IUnitOfWork uow)
    {
        _links = links;
        _players = players;
        _uow = uow;
    }

    public async Task<IReadOnlyList<DeviceLinkDto>> ListAsync(CancellationToken ct = default)
    {
        var links = await _links.ListAsync(ct);
        var names = (await _players.GetByIdsAsync(links.Select(l => l.PlayerId).Distinct().ToList(), ct))
            .ToDictionary(p => p.Id, p => p.Name);
        return links.Select(l => new DeviceLinkDto(l.PlayerId, names.GetValueOrDefault(l.PlayerId, "—"), l.LinkedAtUtc, l.Id, l.DeviceHash)).ToList();
    }

    public async Task ReleaseAsync(int playerId, CancellationToken ct = default)
    {
        var link = await _links.GetByPlayerAsync(playerId, ct) ?? throw new NotFoundException("Ese jugador no tiene ningún dispositivo vinculado.");
        _links.Remove(link);
        await _uow.SaveChangesAsync(ct);
    }
}
