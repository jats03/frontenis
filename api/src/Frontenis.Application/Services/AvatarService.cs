using Frontenis.Application.Dtos;
using Frontenis.Application.Extensions;
using Frontenis.Application.Interfaces.Repositories;
using Frontenis.Application.Interfaces.Services;
using Frontenis.Domain.Exceptions;

namespace Frontenis.Application.Services;

/// <summary>Implementación de <see cref="IAvatarService"/>.</summary>
public sealed class AvatarService : IAvatarService
{
    private readonly IDeviceAccessResolver _access;
    private readonly IPlayerRepository _players;
    private readonly IUnitOfWork _uow;

    public AvatarService(IDeviceAccessResolver access, IPlayerRepository players, IUnitOfWork uow)
    {
        _access = access;
        _players = players;
        _uow = uow;
    }

    public async Task<AvatarDto> SetForDeviceAsync(string? deviceToken, int playerId, int? avatar, CancellationToken ct = default)
    {
        var access = await _access.ResolveAsync(deviceToken, ct);
        if (!access.Allows(playerId)) throw new DomainException("Desde este dispositivo no puedes cambiar el avatar de ese jugador.");
        var player = await _players.GetOrThrowAsync(playerId, ct);
        player.SetAvatar(avatar);
        await _uow.SaveChangesAsync(ct); // vacía la caché de lecturas: la lista de jugadores ya lleva el avatar nuevo
        return new AvatarDto(player.Id, player.AvatarSeed);
    }
}
