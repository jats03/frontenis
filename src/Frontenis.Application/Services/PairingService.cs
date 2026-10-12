using Frontenis.Application.Abstractions;
using Frontenis.Application.Dtos;
using Frontenis.Domain.Entities;
using Frontenis.Domain.Exceptions;
using Frontenis.Domain.Services;

namespace Frontenis.Application.Services;

public interface IPairingService
{
    /// <summary>Calcula un emparejamiento aleatorio sin guardar nada.</summary>
    Task<PairingPreviewDto> PreviewAsync(PlayersSelection selection, CancellationToken ct = default);
    /// <summary>Genera el emparejamiento aleatorio y lo inscribe en la competición.</summary>
    Task<IReadOnlyList<PairDto>> RegisterRandomAsync(int competitionId, PlayersSelection selection, CancellationToken ct = default);
    Task<PairDto> RegisterManualAsync(int competitionId, ManualPairRequest request, CancellationToken ct = default);
}

public sealed class PairingService : IPairingService
{
    private readonly IPlayerRepository _players;
    private readonly ICompetitionRepository _competitions;
    private readonly IPairRepository _pairs;
    private readonly IPairingStrategy _strategy;
    private readonly IUnitOfWork _uow;

    public PairingService(IPlayerRepository players, ICompetitionRepository competitions, IPairRepository pairs,
        IPairingStrategy strategy, IUnitOfWork uow)
    {
        _players = players;
        _competitions = competitions;
        _pairs = pairs;
        _strategy = strategy;
        _uow = uow;
    }

    public async Task<PairingPreviewDto> PreviewAsync(PlayersSelection selection, CancellationToken ct = default)
    {
        var players = await LoadPlayersAsync(selection.PlayerIds, ct);
        var result = _strategy.Generate(players);
        return new PairingPreviewDto(
            result.Pairs.Select(p => new PlayerPairDto(p.First.ToDto(), p.Second.ToDto())).ToList(),
            result.Unpaired?.ToDto());
    }

    public async Task<IReadOnlyList<PairDto>> RegisterRandomAsync(
        int competitionId, PlayersSelection selection, CancellationToken ct = default)
    {
        var competition = await GetCompetitionAsync(competitionId, ct);
        var players = await LoadPlayersAsync(selection.PlayerIds, ct);
        var result = _strategy.Generate(players);

        var entries = new List<CompetitionEntry>();
        foreach (var (first, second) in result.Pairs)
        {
            var pair = new Pair(first, second);
            await _pairs.AddAsync(pair, ct);
            entries.Add(competition.AddPair(pair));
        }
        await _uow.SaveChangesAsync(ct);
        return entries.Select(e => e.ToDto()).ToList();
    }

    public async Task<PairDto> RegisterManualAsync(int competitionId, ManualPairRequest request, CancellationToken ct = default)
    {
        var competition = await GetCompetitionAsync(competitionId, ct);
        var players = await LoadPlayersAsync(new[] { request.Player1Id, request.Player2Id }, ct);
        var pair = new Pair(players[0], players[1]);
        await _pairs.AddAsync(pair, ct);
        var entry = competition.AddPair(pair);
        await _uow.SaveChangesAsync(ct);
        return entry.ToDto();
    }

    private async Task<Competition> GetCompetitionAsync(int id, CancellationToken ct) =>
        await _competitions.GetDetailedAsync(id, ct) ?? throw new NotFoundException("Competición no encontrada.");

    private async Task<IReadOnlyList<Player>> LoadPlayersAsync(IReadOnlyCollection<int> ids, CancellationToken ct)
    {
        var distinct = ids.Distinct().ToList();
        if (distinct.Count != ids.Count) throw new DomainException("Hay jugadores repetidos en la selección.");
        var players = await _players.GetByIdsAsync(distinct, ct);
        if (players.Count != distinct.Count) throw new NotFoundException("Algún jugador no existe.");
        return players;
    }
}
