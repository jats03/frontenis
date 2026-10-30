using Frontenis.Application.Dtos;
using Frontenis.Application.Exceptions;
using Frontenis.Application.Extensions;
using Frontenis.Application.Interfaces.Repositories;
using Frontenis.Application.Interfaces.Services;
using Frontenis.Domain.Entities;
using Frontenis.Domain.Exceptions;

namespace Frontenis.Application.Services;

/// <summary>Implementación de <see cref="IClothingSizeService"/>.</summary>
public sealed class ClothingSizeService : IClothingSizeService
{
    private readonly IDeviceAccessResolver _access;
    private readonly IPlayerRepository _players;
    private readonly IUnitOfWork _uow;

    public ClothingSizeService(IDeviceAccessResolver access, IPlayerRepository players, IUnitOfWork uow)
    {
        _access = access;
        _players = players;
        _uow = uow;
    }

    private static ClothingSizeDto ToDto(Player p) => new(p.Id, p.Name, p.TopSize, p.BottomSize);

    public async Task<IReadOnlyList<ClothingSizeDto>> ListForDeviceAsync(string? deviceToken, CancellationToken ct = default)
    {
        var access = await _access.ResolveAsync(deviceToken, ct);
        var ids = new[] { access.OwnerPlayerId }.Concat(access.DelegateIds).ToList();
        var byId = (await _players.GetByIdsAsync(ids, ct)).ToDictionary(p => p.Id);
        return ids.Where(byId.ContainsKey).Select(id => ToDto(byId[id])).ToList(); // el titular primero
    }

    public async Task<ClothingSizeDto> SaveForDeviceAsync(string? deviceToken, int playerId, SaveClothingSizeRequest request, CancellationToken ct = default)
    {
        var access = await _access.ResolveAsync(deviceToken, ct);
        if (!access.Allows(playerId)) throw new DomainException("Desde este dispositivo no puedes cambiar las tallas de ese jugador.");
        var player = await _players.GetOrThrowAsync(playerId, ct);
        player.SetClothingSizes(request.TopSize, request.BottomSize);
        await _uow.SaveChangesAsync(ct);
        return ToDto(player);
    }

    public async Task<IReadOnlyList<ClothingSizeDto>> ListAllAsync(CancellationToken ct = default) =>
        (await _players.ListAsync(ct)).Where(p => !p.IsReserve).OrderBy(p => p.Name).Select(ToDto).ToList();

    public async Task<IReadOnlyList<ClothingSizeDto>> ListAllForDeviceAsync(string? deviceToken, CancellationToken ct = default)
    {
        var access = await _access.ResolveAsync(deviceToken, ct);
        var owner = await _players.GetByIdAsync(access.OwnerPlayerId, ct);
        if (owner is not { CanViewSizes: true })
            throw new ForbiddenAppException("No tienes acceso al resumen de tallas.");
        return await ListAllAsync(ct);
    }

    public async Task<IReadOnlyList<PlayerRefDto>> ListViewersAsync(CancellationToken ct = default) =>
        (await _players.ListAsync(ct)).Where(p => p.CanViewSizes && !p.IsReserve).OrderBy(p => p.Name)
            .Select(p => new PlayerRefDto(p.Id, p.Name)).ToList();

    public async Task<IReadOnlyList<PlayerRefDto>> SetViewerAsync(int playerId, bool allowed, CancellationToken ct = default)
    {
        var player = await _players.GetOrThrowAsync(playerId, ct);
        if (player.IsReserve) throw new DomainException("Ese jugador no es un jugador real.");
        if (player.CanViewSizes != allowed)
        {
            player.SetCanViewSizes(allowed);
            await _uow.SaveChangesAsync(ct);
        }
        return await ListViewersAsync(ct);
    }
}
