using Frontenis.Application.Dtos;
using Frontenis.Application.Extensions;
using Frontenis.Application.Interfaces.Repositories;
using Frontenis.Application.Interfaces.Services;
using Frontenis.Application.Mapping;
using Frontenis.Domain.Entities;
using Frontenis.Domain.Exceptions;
using Frontenis.Domain.Services;

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
        var name = PlayerNameRules.Validate(request.Name);
        await EnsureNameIsFreeAsync(name, null, ct);
        var player = new Player(name, request.Position, request.GroupNumber);
        await _players.AddAsync(player, ct);
        await _uow.SaveChangesAsync(ct);
        return player.ToDto();
    }

    public async Task<PlayerDto> UpdateAsync(int id, PlayerRequest request, CancellationToken ct = default)
    {
        var player = await _players.GetOrThrowAsync(id, ct);
        var name = PlayerNameRules.Validate(request.Name);
        // Solo se comprueba si el nombre cambia de verdad (cambiar de posición o de grupo reenvía el mismo nombre);
        // corregir mayúsculas, tildes o espacios del propio nombre tampoco choca con nadie.
        if (!PlayerNameRules.Same(name, player.Name)) await EnsureNameIsFreeAsync(name, player.Id, ct);
        player.Update(name, request.Position, request.GroupNumber);
        await _uow.SaveChangesAsync(ct);
        return player.ToDto();
    }

    /// <summary>Lanza un error si otro jugador (o el jugador reserva) ya se llama así, sin contar espacios, mayúsculas ni tildes.</summary>
    private async Task EnsureNameIsFreeAsync(string name, int? exceptId, CancellationToken ct)
    {
        if (PlayerNameRules.Same(name, Player.ReserveName))
            throw new DomainException($"El nombre «{Player.ReserveName}» está reservado.");
        var clash = (await _players.ListAsync(ct)).FirstOrDefault(p => p.Id != exceptId && PlayerNameRules.Same(p.Name, name));
        if (clash is not null)
            throw new DomainException(clash.Name == name
                ? $"Ya existe un jugador llamado «{clash.Name}»."
                : $"Ya existe un jugador llamado «{clash.Name}» (no se distinguen espacios, mayúsculas, tildes ni guiones).");
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
