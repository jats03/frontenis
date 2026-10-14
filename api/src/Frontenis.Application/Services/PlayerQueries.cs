using Frontenis.Application.Dtos;
using Frontenis.Application.Interfaces.Repositories;
using Frontenis.Application.Interfaces.Services;
using Frontenis.Application.Mapping;

namespace Frontenis.Application.Services;

/// <summary>Implementación de <see cref="Interfaces.Services.IPlayerQueries"/>.</summary>
public sealed class PlayerQueries : IPlayerQueries
{
    private readonly IPlayerRepository _players;

    public PlayerQueries(IPlayerRepository players) { _players = players; }

    public async Task<IReadOnlyList<PlayerDto>> ListAsync(CancellationToken ct = default) =>
        (await _players.ListAsync(ct)).Select(p => p.ToDto()).ToList();
}
