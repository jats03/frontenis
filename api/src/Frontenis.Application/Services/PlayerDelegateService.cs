using Frontenis.Application.Dtos;
using Frontenis.Application.Extensions;
using Frontenis.Application.Interfaces.Repositories;
using Frontenis.Application.Interfaces.Services;
using Frontenis.Domain.Entities;
using Frontenis.Domain.Exceptions;

namespace Frontenis.Application.Services;

/// <summary>Consulta y gestión de apadrinados (<see cref="IPlayerDelegateQueries"/> y <see cref="IPlayerDelegateCommands"/>).</summary>
public sealed class PlayerDelegateService : IPlayerDelegateQueries, IPlayerDelegateCommands
{
    private readonly IPlayerDelegateRepository _delegates;
    private readonly IPlayerRepository _players;
    private readonly IUnitOfWork _uow;

    public PlayerDelegateService(IPlayerDelegateRepository delegates, IPlayerRepository players, IUnitOfWork uow)
    {
        _delegates = delegates;
        _players = players;
        _uow = uow;
    }

    public async Task<IReadOnlyList<PlayerDelegateDto>> ListAsync(CancellationToken ct = default)
    {
        var all = await _delegates.ListAsync(ct);
        var ids = all.SelectMany(d => new[] { d.OwnerPlayerId, d.DelegatePlayerId }).Distinct().ToList();
        var names = (await _players.GetByIdsAsync(ids, ct)).ToDictionary(p => p.Id, p => p.Name);
        return all.Select(d => ToDto(d, names)).OrderBy(d => d.OwnerName).ThenBy(d => d.DelegateName).ToList();
    }

    public async Task<PlayerDelegateDto> AddAsync(AddPlayerDelegateRequest r, CancellationToken ct = default)
    {
        var owner = await _players.GetOrThrowAsync(r.OwnerPlayerId, ct);
        var apadrinado = await _players.GetOrThrowAsync(r.DelegatePlayerId, ct);
        var created = PlayerDelegate.Create(r.OwnerPlayerId, r.DelegatePlayerId);
        if (await _delegates.GetAsync(r.OwnerPlayerId, r.DelegatePlayerId, ct) is not null)
            throw new DomainException("Ese apadrinamiento ya existe.");
        await _delegates.AddAsync(created, ct);
        await _uow.SaveChangesAsync(ct);
        return new PlayerDelegateDto(owner.Id, owner.Name, apadrinado.Id, apadrinado.Name);
    }

    public async Task RemoveAsync(int ownerPlayerId, int delegatePlayerId, CancellationToken ct = default)
    {
        _delegates.Remove(await _delegates.GetOrThrowAsync(ownerPlayerId, delegatePlayerId, ct));
        await _uow.SaveChangesAsync(ct);
    }

    private static PlayerDelegateDto ToDto(PlayerDelegate d, IReadOnlyDictionary<int, string> names) =>
        new(d.OwnerPlayerId, names.GetValueOrDefault(d.OwnerPlayerId, "—"), d.DelegatePlayerId, names.GetValueOrDefault(d.DelegatePlayerId, "—"));
}
