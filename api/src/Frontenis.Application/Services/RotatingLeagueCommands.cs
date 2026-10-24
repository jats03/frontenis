using Frontenis.Application.Dtos;
using Frontenis.Application.Extensions;
using Frontenis.Application.Interfaces.Repositories;
using Frontenis.Application.Interfaces.Services;
using Frontenis.Application.Mapping;
using Frontenis.Domain.Entities;
using Frontenis.Domain.Interfaces;

namespace Frontenis.Application.Services;

/// <summary>Implementación de <see cref="IRotatingLeagueCommands"/>: carga la liga, delega la regla en el dominio y guarda.</summary>
public sealed class RotatingLeagueCommands : IRotatingLeagueCommands
{
    private readonly ICompetitionRepository _competitions;
    private readonly IPlayerRepository _players;
    private readonly IUnitOfWork _uow;
    private readonly IRotationPlanner _planner;

    public RotatingLeagueCommands(ICompetitionRepository competitions, IPlayerRepository players, IUnitOfWork uow, IRotationPlanner planner)
    {
        _competitions = competitions;
        _players = players;
        _uow = uow;
        _planner = planner;
    }

    public async Task<CompetitionDetailDto> AddPlayerAsync(int competitionId, AddRotatingPlayerRequest request, CancellationToken ct = default)
    {
        var competition = await _competitions.GetDetailedOrThrowAsync(competitionId, ct);
        var player = await _players.GetOrThrowAsync(request.PlayerId, ct);
        if (request.GroupId.HasValue) competition.AddPlayerToGroup(request.GroupId.Value, player);
        else competition.AddPlayerToLastGroup(player);
        return await SaveAsync(competition, Array.Empty<Pair>(), ct);
    }

    public async Task<CompetitionDetailDto> RemovePlayerAsync(int competitionId, int groupId, int playerId, CancellationToken ct = default)
    {
        var competition = await _competitions.GetDetailedOrThrowAsync(competitionId, ct);
        var removed = competition.RemovePlayerFromGroup(groupId, playerId);
        return await SaveAsync(competition, removed, ct);
    }

    public async Task<CompetitionDetailDto> MoveMatchAsync(int matchId, MoveMatchRequest request, CancellationToken ct = default)
    {
        var competition = await _competitions.GetDetailedByMatchOrThrowAsync(matchId, ct);
        competition.MoveMatchToRound(matchId, request.Round);
        return await SaveAsync(competition, Array.Empty<Pair>(), ct);
    }

    public async Task<CompetitionDetailDto> RemoveMatchAsync(int matchId, CancellationToken ct = default)
    {
        var competition = await _competitions.GetDetailedByMatchOrThrowAsync(matchId, ct);
        var removed = competition.RemoveMatch(matchId);
        return await SaveAsync(competition, removed, ct);
    }

    public async Task<CompetitionDetailDto> AddMatchAsync(int competitionId, int groupId, AddMatchRequest request, CancellationToken ct = default)
    {
        var competition = await _competitions.GetDetailedOrThrowAsync(competitionId, ct);
        var usesReserve = new[] { request.Home1Id, request.Home2Id, request.Away1Id, request.Away2Id }.Contains(0);
        var reserve = usesReserve ? await _players.GetOrCreateReserveAsync(ct) : null;
        competition.AddMatchToRound(groupId, request.Round, request.Home1Id, request.Home2Id, request.Away1Id, request.Away2Id, reserve);
        return await SaveAsync(competition, Array.Empty<Pair>(), ct);
    }

    public async Task<CompetitionDetailDto> SwapMatchesAsync(int matchId, SwapMatchRequest request, CancellationToken ct = default)
    {
        var competition = await _competitions.GetDetailedByMatchOrThrowAsync(matchId, ct);
        competition.SwapMatchRounds(matchId, request.OtherMatchId);
        return await SaveAsync(competition, Array.Empty<Pair>(), ct);
    }

    public async Task<CompetitionDetailDto> SwapRoundsAsync(int competitionId, int groupId, SwapRoundsRequest request, CancellationToken ct = default)
    {
        var competition = await _competitions.GetDetailedOrThrowAsync(competitionId, ct);
        competition.SwapRounds(groupId, request.RoundA, request.RoundB);
        return await SaveAsync(competition, Array.Empty<Pair>(), ct);
    }

    public async Task<CompetitionDetailDto> RemoveRoundAsync(int competitionId, int groupId, int round, CancellationToken ct = default)
    {
        var competition = await _competitions.GetDetailedOrThrowAsync(competitionId, ct);
        var removed = competition.RemoveRound(groupId, round);
        return await SaveAsync(competition, removed, ct);
    }

    private async Task<CompetitionDetailDto> SaveAsync(Competition competition, IReadOnlyList<Pair> removedPairs, CancellationToken ct)
    {
        await _uow.SaveChangesAsync(ct);
        // Las parejas de una liga rotatoria solo existen para sus partidos: si ya no tienen, se eliminan.
        await _competitions.DeleteOrphanPairsAsync(removedPairs.Select(p => p.Id).Where(id => id != 0).ToList(), ct);
        return competition.ToDetailDto();
    }
}
