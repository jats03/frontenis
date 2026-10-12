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
    Task<CompetitionDetailDto> DrawAsync(int id, CancellationToken ct = default);
    Task<CompetitionDetailDto> GeneratePlayoffsAsync(int id, CancellationToken ct = default);
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

    public CompetitionService(ICompetitionRepository repository, IGroupDistributor distributor,
        IScheduleGenerator scheduler, IStandingsCalculator calculator, IBracketGenerator bracket, IUnitOfWork uow)
    {
        _repository = repository;
        _distributor = distributor;
        _scheduler = scheduler;
        _calculator = calculator;
        _bracket = bracket;
        _uow = uow;
    }

    public async Task<IReadOnlyList<CompetitionSummaryDto>> ListAsync(CancellationToken ct = default) =>
        (await _repository.ListAsync(ct)).Select(c => c.ToSummary()).ToList();

    public async Task<CompetitionDetailDto> GetAsync(int id, CancellationToken ct = default) =>
        ToDetail(await LoadAsync(id, ct));

    public async Task<CompetitionSummaryDto> CreateAsync(CreateCompetitionRequest r, CancellationToken ct = default)
    {
        var competition = Competition.Create(new CompetitionSettings(
            r.Name, r.Type, r.PairCount, r.GroupCount, r.PairsPerGroup,
            r.PointsPerWin, r.PointsPerLoss, r.DoubleRound, r.QualifiersPerGroup));
        await _repository.AddAsync(competition, ct);
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

    public async Task<CompetitionDetailDto> GeneratePlayoffsAsync(int id, CancellationToken ct = default)
    {
        var competition = await LoadAsync(id, ct);
        competition.GeneratePlayoffs(_calculator, _bracket);
        await _uow.SaveChangesAsync(ct);
        return ToDetail(competition);
    }

    public async Task<IReadOnlyList<GroupStandingsDto>> GetStandingsAsync(int id, CancellationToken ct = default)
    {
        var competition = await LoadAsync(id, ct);
        var result = new List<GroupStandingsDto>();

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
