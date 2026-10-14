using Frontenis.Application.Dtos;
using Frontenis.Application.Extensions;
using Frontenis.Application.Interfaces.Repositories;
using Frontenis.Application.Interfaces.Services;
using Frontenis.Application.Mapping;
using Frontenis.Domain.Entities;
using Frontenis.Domain.Exceptions;
using Frontenis.Domain.Interfaces;
using Frontenis.Domain.Services;

namespace Frontenis.Application.Services;

public sealed class CompetitionDrawService : ICompetitionDrawService
{
    private readonly ICompetitionRepository _repository;
    private readonly IPlayerRepository _players;
    private readonly IUnitOfWork _uow;
    private readonly IGroupDistributor _distributor;
    private readonly IScheduleGenerator _scheduler;
    private readonly IStandingsCalculator _calculator;
    private readonly IBracketGenerator _bracket;
    private readonly IRandomizer _randomizer;
    private readonly IRotationPlanner _planner;
    private readonly IIndividualStandingsCalculator _individual;

    public CompetitionDrawService(ICompetitionRepository repository, IPlayerRepository players, IUnitOfWork uow,
        IGroupDistributor distributor, IScheduleGenerator scheduler, IStandingsCalculator calculator,
        IBracketGenerator bracket, IRandomizer randomizer, IRotationPlanner planner,
        IIndividualStandingsCalculator individual)
    {
        _repository = repository;
        _players = players;
        _uow = uow;
        _distributor = distributor;
        _scheduler = scheduler;
        _calculator = calculator;
        _bracket = bracket;
        _randomizer = randomizer;
        _planner = planner;
        _individual = individual;
    }

    public async Task<CompetitionDetailDto> DrawAsync(int id, CancellationToken ct = default)
    {
        var competition = await _repository.GetDetailedOrThrowAsync(id, ct);
        competition.Draw(_distributor, _scheduler);
        return await SaveAsync(competition, ct);
    }

    public async Task<CompetitionDetailDto> DrawManualAsync(int id, ManualDrawRequest request, CancellationToken ct = default)
    {
        var competition = await _repository.GetDetailedOrThrowAsync(id, ct);
        competition.DrawManual(request.Groups ?? Array.Empty<IReadOnlyList<int>>(), _scheduler);
        return await SaveAsync(competition, ct);
    }

    public async Task<CompetitionDetailDto> GeneratePlayoffsAsync(int id, CancellationToken ct = default)
    {
        var competition = await _repository.GetDetailedOrThrowAsync(id, ct);
        competition.GeneratePlayoffs(_calculator, _bracket);
        return await SaveAsync(competition, ct);
    }

    public async Task<CompetitionDetailDto> DrawRotatingAsync(int id, PlayersSelection selection, CancellationToken ct = default)
    {
        var competition = await _repository.GetDetailedOrThrowAsync(id, ct);
        var ids = selection.PlayerIds ?? Array.Empty<int>();
        if (ids.Distinct().Count() != ids.Count) throw new DomainException("Hay jugadores repetidos en la selección.");
        var players = await _players.GetByIdsAsync(ids.ToList(), ct);
        if (players.Count != ids.Count) throw new NotFoundException("Algún jugador no existe.");
        competition.DrawRotating(players, _randomizer, _planner);
        return await SaveAsync(competition, ct);
    }

    public async Task<CompetitionDetailDto> DrawRotatingManualAsync(int id, ManualDrawRequest request, CancellationToken ct = default)
    {
        var competition = await _repository.GetDetailedOrThrowAsync(id, ct);
        var groups = request.Groups ?? Array.Empty<IReadOnlyList<int>>();
        var ids = groups.SelectMany(g => g).Distinct().ToList();
        var players = await _players.GetByIdsAsync(ids, ct);
        if (players.Count != ids.Count) throw new NotFoundException("Algún jugador no existe.");
        var byId = players.ToDictionary(p => p.Id);
        competition.DrawRotatingManual(
            groups.Select(g => (IReadOnlyList<Player>)g.Select(pid => byId[pid]).ToList()).ToList(), _planner);
        return await SaveAsync(competition, ct);
    }

    public async Task<CompetitionSummaryDto> CreateNextLeagueAsync(int id, CancellationToken ct = default)
    {
        var previous = await _repository.GetDetailedOrThrowAsync(id, ct);
        var tables = previous.RotatingStandings(_individual);
        var moves = PromotionRules.Compute(tables.Select(t => t.Rows).ToList());

        var groups = previous.Groups.OrderBy(g => g.Id).ToList();
        var playersById = groups.SelectMany(previous.GroupPlayers).GroupBy(p => p.Id).ToDictionary(g => g.Key, g => g.First());
        var rosters = PromotionRules.NextRosters(
                groups.Select(g => (IReadOnlyList<int>)previous.GroupPlayers(g).Select(p => p.Id).ToList()).ToList(), moves)
            .Select(r => (IReadOnlyList<Player>)r.Select(pid => playersById[pid]).ToList())
            .ToList();

        var next = Competition.CreateNext(previous, rosters, _planner);
        await _repository.AddAsync(next, ct);
        await _uow.SaveChangesAsync(ct);
        previous.SetNextCompetition(next.Id); // necesita el id que se genera al guardar la nueva liga
        await _uow.SaveChangesAsync(ct);
        return next.ToSummary();
    }

    private async Task<CompetitionDetailDto> SaveAsync(Competition competition, CancellationToken ct)
    {
        await _uow.SaveChangesAsync(ct);
        return competition.ToDetailDto();
    }
}
