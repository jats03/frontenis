using Frontenis.Application.Abstractions;
using Frontenis.Application.Dtos;
using Frontenis.Domain.Entities;
using Frontenis.Domain.Exceptions;

namespace Frontenis.Application.Services;

public interface IPlayerService
{
    Task<IReadOnlyList<PlayerDto>> ListAsync(CancellationToken ct = default);
    Task<PlayerDto> CreateAsync(PlayerRequest request, CancellationToken ct = default);
    Task<PlayerDto> UpdateAsync(int id, PlayerRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

public sealed class PlayerService : IPlayerService
{
    private readonly IPlayerRepository _players;
    private readonly IUnitOfWork _uow;

    public PlayerService(IPlayerRepository players, IUnitOfWork uow)
    {
        _players = players;
        _uow = uow;
    }

    public async Task<IReadOnlyList<PlayerDto>> ListAsync(CancellationToken ct = default) =>
        (await _players.ListAsync(ct)).Select(p => p.ToDto()).ToList();

    public async Task<PlayerDto> CreateAsync(PlayerRequest request, CancellationToken ct = default)
    {
        var player = new Player(request.Name, request.Position, request.GroupNumber);
        await _players.AddAsync(player, ct);
        await _uow.SaveChangesAsync(ct);
        return player.ToDto();
    }

    public async Task<PlayerDto> UpdateAsync(int id, PlayerRequest request, CancellationToken ct = default)
    {
        var player = await _players.GetByIdAsync(id, ct) ?? throw new NotFoundException("Jugador no encontrado.");
        player.Update(request.Name, request.Position, request.GroupNumber);
        await _uow.SaveChangesAsync(ct);
        return player.ToDto();
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var player = await _players.GetByIdAsync(id, ct) ?? throw new NotFoundException("Jugador no encontrado.");
        if (await _players.IsInUseAsync(id, ct))
            throw new DomainException("El jugador pertenece a una pareja de alguna competición y no se puede borrar.");
        _players.Remove(player);
        await _uow.SaveChangesAsync(ct);
    }
}
