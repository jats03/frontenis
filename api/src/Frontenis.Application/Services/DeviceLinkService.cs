using Frontenis.Application.Dtos;
using Frontenis.Application.Extensions;
using Frontenis.Application.Interfaces.Repositories;
using Frontenis.Application.Interfaces.Security;
using Frontenis.Application.Interfaces.Services;
using Frontenis.Domain.Entities;
using Frontenis.Domain.Exceptions;

namespace Frontenis.Application.Services;

/// <summary>Implementación de <see cref="Interfaces.Services.IDeviceLinkService"/>.</summary>
public sealed class DeviceLinkService : IDeviceLinkService
{
    private readonly IDeviceTokenHasher _hasher;
    private readonly IDeviceAccessResolver _access;
    private readonly IDeviceLinkRepository _links;
    private readonly IPlayerRepository _players;
    private readonly IUnitOfWork _uow;

    public DeviceLinkService(IDeviceTokenHasher hasher, IDeviceAccessResolver access, IDeviceLinkRepository links,
        IPlayerRepository players, IUnitOfWork uow)
    {
        _hasher = hasher;
        _access = access;
        _links = links;
        _players = players;
        _uow = uow;
    }

    public async Task<DeviceMeDto?> GetMeAsync(string? deviceToken, CancellationToken ct = default)
    {
        var access = await _access.FindAsync(deviceToken, ct);
        if (access is null) return null;
        var ids = new[] { access.OwnerPlayerId }.Concat(access.DelegateIds).ToList();
        var found = (await _players.GetByIdsAsync(ids, ct)).ToDictionary(p => p.Id);
        PlayerRefDto Ref(int id) => new(id, found.TryGetValue(id, out var p) ? p.Name : "—");
        var canViewSizes = found.TryGetValue(access.OwnerPlayerId, out var owner) && owner.CanViewSizes;
        return new DeviceMeDto(Ref(access.OwnerPlayerId), access.DelegateIds.Select(Ref).ToList(), canViewSizes);
    }

    public async Task<IReadOnlyList<PlayerRefDto>> ListAvailablePlayersAsync(CancellationToken ct = default)
    {
        var taken = (await _links.ListAsync(ct)).Select(l => l.PlayerId).ToHashSet();
        return (await _players.ListAsync(ct)).Where(p => !taken.Contains(p.Id)).Select(p => new PlayerRefDto(p.Id, p.Name)).ToList();
    }

    public async Task<IReadOnlyList<DevicePlayerDto>> ListSelectablePlayersAsync(CancellationToken ct = default)
    {
        var taken = (await _links.ListAsync(ct)).Select(l => l.PlayerId).ToHashSet();
        return (await _players.ListAsync(ct)).Select(p => new DevicePlayerDto(p.Id, p.Name, taken.Contains(p.Id))).ToList();
    }

    public async Task<DeviceMeDto> LinkAsync(string? deviceToken, int playerId, bool replace = false, CancellationToken ct = default)
    {
        if (!_hasher.IsValid(deviceToken)) throw new DomainException("Identificador de dispositivo no válido.");
        var hash = _hasher.Hash(deviceToken!);
        await _players.GetOrThrowAsync(playerId, ct);

        var current = await _links.GetByHashAsync(hash, ct);
        if (current is not null)
        {
            if (current.PlayerId != playerId)
                throw new DomainException("Este dispositivo ya está vinculado a otro jugador. Desvincúlalo antes de elegir otro.");
        }
        else
        {
            var other = await _links.GetByPlayerAsync(playerId, ct);
            if (other is not null)
            {
                if (!replace)
                    throw new DomainException("Este jugador ya está vinculado a otro dispositivo. Confirma que eres tú para quedarte con él.");
                _links.Remove(other); // el móvil anterior (o este mismo con los datos borrados) deja de estar identificado
                await _uow.SaveChangesAsync(ct);
            }
            await _links.AddAsync(DeviceLink.Create(hash, playerId), ct);
            await _uow.SaveChangesAsync(ct);
        }
        return (await GetMeAsync(deviceToken, ct))!;
    }

    public async Task UnlinkAsync(string? deviceToken, CancellationToken ct = default)
    {
        if (!_hasher.IsValid(deviceToken)) return;
        var link = await _links.GetByHashAsync(_hasher.Hash(deviceToken!), ct);
        if (link is null) return;
        _links.Remove(link);
        await _uow.SaveChangesAsync(ct);
    }
}
