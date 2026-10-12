using Frontenis.Application.Abstractions;
using Frontenis.Application.Dtos;
using Frontenis.Domain.Entities;
using Frontenis.Domain.Enums;
using Frontenis.Domain.Exceptions;
using Frontenis.Domain.Services;

namespace Frontenis.Application.Services;

public interface ICompetitionService
{
    Task<IReadOnlyList<CompetitionSummaryDto>> ListAsync(CancellationToken ct = default);
    Task<CompetitionDetailDto> GetAsync(int id, CancellationToken ct = default);
    Task<CompetitionSummaryDto> CreateAsync(CreateCompetitionRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task<CompetitionSummaryDto> RenameAsync(int id, RenameCompetitionRequest request, CancellationToken ct = default);
    Task<CompetitionDetailDto> DrawAsync(int id, CancellationToken ct = default);
    Task<CompetitionDetailDto> DrawManualAsync(int id, ManualDrawRequest request, CancellationToken ct = default);
    Task<CompetitionDetailDto> DrawRotatingManualAsync(int id, ManualDrawRequest request, CancellationToken ct = default);
    Task<CompetitionDetailDto> GeneratePlayoffsAsync(int id, CancellationToken ct = default);
    /// <summary>Liga rotatoria: reparte los jugadores en grupos y genera las jornadas con parejas cambiantes.</summary>
    Task<CompetitionDetailDto> DrawRotatingAsync(int id, PlayersSelection selection, CancellationToken ct = default);
    /// <summary>Liga rotatoria terminada: crea la siguiente con ascensos y descensos aplicados.</summary>
    Task<CompetitionSummaryDto> CreateNextLeagueAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<GroupStandingsDto>> GetStandingsAsync(int id, CancellationToken ct = default);
}

public sealed class CompetitionService : ICompetitionService
{
    private readonly ICompetitionRepository _repository;
    private readonly IGroupDistributor _distributor;
    private readonly IScheduleGenerator _scheduler;
    private readonly IStandingsCalculator _calculator;
    private readonly IBracketGenerator _bracket;
    private readonly IUnitOfWork _uow;
    private readonly IPlayerRepository _players;
    private readonly IRandomizer _randomizer;
    private readonly IRotationPlanner _planner;
    private readonly IIndividualStandingsCalculator _individual;

    public CompetitionService(ICompetitionRepository repository, IGroupDistributor distributor,
        IScheduleGenerator scheduler, IStandingsCalculator calculator, IBracketGenerator bracket, IUnitOfWork uow,
        IPlayerRepository players, IRandomizer randomizer, IRotationPlanner planner,
        IIndividualStandingsCalculator individual)
    {
        _repository = repository;
        _distributor = distributor;
        _scheduler = scheduler;
        _calculator = calculator;
        _bracket = bracket;
        _uow = uow;
        _players = players;
        _randomizer = randomizer;
        _planner = planner;
        _individual = individual;
    }

    public async Task<IReadOnlyList<CompetitionSummaryDto>> ListAsync(CancellationToken ct = default) =>
        (await _repository.ListAsync(ct)).Select(c => c.ToSummary()).ToList();

    public async Task<CompetitionDetailDto> GetAsync(int id, CancellationToken ct = default) =>
        ToDetail(await LoadAsync(id, ct));

    public async Task<CompetitionSummaryDto> CreateAsync(CreateCompetitionRequest r, CancellationToken ct = default)
    {
        var competition = Competition.Create(new CompetitionSettings(
            r.Name, r.Type, r.PairCount, r.GroupCount, r.PairsPerGroup,
            r.PointsPerWin, r.PointsPerLoss, r.DoubleRound, r.QualifiersPerGroup, r.Rounds));
        await _repository.AddAsync(competition, ct);
        await _uow.SaveChangesAsync(ct);
        return competition.ToSummary();
    }

    public async Task<CompetitionSummaryDto> RenameAsync(int id, RenameCompetitionRequest request, CancellationToken ct = default)
    {
        var competition = await LoadAsync(id, ct);
        competition.Rename(request.Name);
        await _uow.SaveChangesAsync(ct);
        return competition.ToSummary();
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        await LoadAsync(id, ct);
        await _repository.DeleteAsync(id, ct);
    }

    public async Task<CompetitionDetailDto> DrawAsync(int id, CancellationToken ct = default)
    {
        var competition = await LoadAsync(id, ct);
        competition.Draw(_distributor, _scheduler);
        await _uow.SaveChangesAsync(ct);
        return ToDetail(competition);
    }

    public async Task<CompetitionDetailDto> DrawManualAsync(int id, ManualDrawRequest request, CancellationToken ct = default)
    {
        var competition = await LoadAsync(id, ct);
        competition.DrawManual(request.Groups ?? Array.Empty<IReadOnlyList<int>>(), _scheduler);
        await _uow.SaveChangesAsync(ct);
        return ToDetail(competition);
    }

    public async Task<CompetitionDetailDto> DrawRotatingManualAsync(int id, ManualDrawRequest request, CancellationToken ct = default)
    {
        var competition = await LoadAsync(id, ct);
        var groups = request.Groups ?? Array.Empty<IReadOnlyList<int>>();
        var ids = groups.SelectMany(g => g).ToList();
        var players = await _players.GetByIdsAsync(ids.Distinct().ToList(), ct);
        if (players.Count != ids.Distinct().Count()) throw new NotFoundException("Algún jugador no existe.");
        var byId = players.ToDictionary(p => p.Id);
        competition.DrawRotatingManual(
            groups.Select(g => (IReadOnlyList<Player>)g.Select(pid => byId[pid]).ToList()).ToList(), _planner);
        await _uow.SaveChangesAsync(ct);
        return ToDetail(competition);
    }

    public async Task<CompetitionDetailDto> GeneratePlayoffsAsync(int id, CancellationToken ct = default)
    {
        var competition = await LoadAsync(id, ct);
        competition.GeneratePlayoffs(_calculator, _bracket);
        await _uow.SaveChangesAsync(ct);
        return ToDetail(competition);
    }

    public async Task<CompetitionDetailDto> DrawRotatingAsync(int id, PlayersSelection selection, CancellationToken ct = default)
    {
        var competition = await LoadAsync(id, ct);
        var ids = selection.PlayerIds ?? Array.Empty<int>();
        if (ids.Distinct().Count() != ids.Count) throw new DomainException("Hay jugadores repetidos en la selección.");
        var players = await _players.GetByIdsAsync(ids.ToList(), ct);
        if (players.Count != ids.Count) throw new NotFoundException("Algún jugador no existe.");
        competition.DrawRotating(players, _randomizer, _planner);
        await _uow.SaveChangesAsync(ct);
        return ToDetail(competition);
    }

    public async Task<CompetitionSummaryDto> CreateNextLeagueAsync(int id, CancellationToken ct = default)
    {
        var previous = await LoadAsync(id, ct);
        var groups = previous.Groups.OrderBy(g => g.Id).ToList();
        var standings = RotatingStandings(previous, groups);
        var moves = PromotionRules.Compute(standings.Select(s => s.Rows).ToList());

        var playersById = groups.SelectMany(previous.GroupPlayers).GroupBy(p => p.Id).ToDictionary(g => g.Key, g => g.First());
        var rosters = PromotionRules.NextRosters(
                groups.Select(g => (IReadOnlyList<int>)previous.GroupPlayers(g).Select(p => p.Id).ToList()).ToList(), moves)
            .Select(r => (IReadOnlyList<Player>)r.Select(pid => playersById[pid]).ToList())
            .ToList();

        var next = Competition.CreateNext(previous, rosters, _planner);
        await _repository.AddAsync(next, ct);
        await _uow.SaveChangesAsync(ct);
        previous.SetNextCompetition(next.Id);
        await _uow.SaveChangesAsync(ct);
        return next.ToSummary();
    }

    private (int GroupId, string GroupName, IReadOnlyList<StandingRow> Rows)[] RotatingStandings(
        Competition competition, IReadOnlyList<Group> groups) =>
        groups.Select(g => (g.Id, g.Name, _individual.Calculate(
            competition.GroupPlayers(g).Select(p => p.Id).ToList(), competition.GroupTeamOutcomes(g),
            competition.PointsPerWin, competition.PointsPerLoss))).ToArray();

    public async Task<IReadOnlyList<GroupStandingsDto>> GetStandingsAsync(int id, CancellationToken ct = default)
    {
        var competition = await LoadAsync(id, ct);
        var result = new List<GroupStandingsDto>();

        if (competition.IsRotating)
        {
            var groupList = competition.Groups.OrderBy(g => g.Id).ToList();
            var tables = RotatingStandings(competition, groupList);
            var finished = competition.Status == CompetitionStatus.Finished;
            var movement = new Dictionary<int, int>();
            if (finished)
                foreach (var m in PromotionRules.Compute(tables.Select(t => t.Rows).ToList()))
                    movement[m.PlayerId] = m.IsPromotion ? 1 : -1;

            foreach (var (groupId, groupName, rows) in tables)
            {
                var names = competition.GroupPlayers(groupList.First(g => g.Id == groupId)).ToDictionary(p => p.Id, p => p.Name);
                result.Add(new GroupStandingsDto(groupId, groupName, rows.Select(row => new StandingDto(
                    row.Rank, row.EntryId, names[row.EntryId], row.Played, row.Won, row.Lost,
                    row.PointsFor, row.PointsAgainst, row.PointsDiff, row.ClassificationPoints, false,
                    movement.GetValueOrDefault(row.EntryId))).ToList()));
            }
            return result;
        }

        foreach (var group in competition.Groups.OrderBy(g => g.Id))
        {
            var entries = competition.Entries.Where(e => e.GroupId == group.Id).ToList();
            var rows = _calculator.Calculate(
                entries.Select(e => e.Id).ToList(), competition.GroupOutcomes(group),
                competition.PointsPerWin, competition.PointsPerLoss);

            var dtos = rows.Select(row => new StandingDto(
                row.Rank, row.EntryId, entries.First(e => e.Id == row.EntryId).Pair.DisplayName,
                row.Played, row.Won, row.Lost, row.PointsFor, row.PointsAgainst, row.PointsDiff,
                row.ClassificationPoints,
                competition.Type == CompetitionType.Tournament && row.Rank <= competition.QualifiersPerGroup))
                .ToList();
            result.Add(new GroupStandingsDto(group.Id, group.Name, dtos));
        }
        return result;
    }

    private async Task<Competition> LoadAsync(int id, CancellationToken ct) =>
        await _repository.GetDetailedAsync(id, ct) ?? throw new NotFoundException("Competición no encontrada.");

    private static CompetitionDetailDto ToDetail(Competition c) => new(
        c.ToSummary(),
        c.Groups.OrderBy(g => g.Id).Select(g => new GroupDto(g.Id, g.Name)).ToList(),
        c.Entries.OrderBy(e => e.Id).Select(e => e.ToDto()).ToList(),
        c.Matches.OrderBy(m => m.Phase).ThenBy(m => m.GroupId).ThenBy(m => m.Round).ThenBy(m => m.Order)
            .Select(m => m.ToDto()).ToList());
}
