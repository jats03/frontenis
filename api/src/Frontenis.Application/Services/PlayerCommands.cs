using Frontenis.Application.Dtos;
using Frontenis.Application.Extensions;
using Frontenis.Application.Interfaces.Repositories;
using Frontenis.Application.Interfaces.Services;
using Frontenis.Application.Mapping;
using Frontenis.Domain.Entities;
using Frontenis.Domain.Exceptions;

namespace Frontenis.Application.Services;

/// <summary>Implementación de <see cref="Interfaces.Services.IPlayerCommands"/>.</summary>
public sealed class PlayerCommands : IPlayerCommands
{
    private readonly IPlayerRepository _players;
    private readonly IUnitOfWork _uow;

    public PlayerCommands(IPlayerRepository players, IUnitOfWork uow)
    {
        _players = players;
        _uow = uow;
    }

    public async Task<PlayerDto> CreateAsync(PlayerRequest request, CancellationToken ct = default)
    {
        var player = new Player(request.Name, request.Position, request.GroupNumber);
        await _players.AddAsync(player, ct);
        await _uow.SaveChangesAsync(ct);
        return player.ToDto();
    }

    public async Task<PlayerDto> UpdateAsync(int id, PlayerRequest request, CancellationToken ct = default)
    {
        var player = await _players.GetOrThrowAsync(id, ct);
        player.Update(request.Name, request.Position, request.GroupNumber);
        await _uow.SaveChangesAsync(ct);
        return player.ToDto();
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var player = await _players.GetOrThrowAsync(id, ct);
        if (await _players.IsInUseAsync(id, ct))
            throw new DomainException("El jugador pertenece a una pareja de alguna competición y no se puede borrar.");
        _players.Remove(player);
        await _uow.SaveChangesAsync(ct);
    }
}
