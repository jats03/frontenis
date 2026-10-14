using Frontenis.Application.Dtos;
using Frontenis.Application.Extensions;
using Frontenis.Application.Interfaces.Repositories;
using Frontenis.Application.Interfaces.Services;
using Frontenis.Application.Mapping;
using Frontenis.Domain.Entities;
using Frontenis.Domain.Enums;
using Frontenis.Domain.Interfaces;
using Frontenis.Domain.Models;
using Frontenis.Domain.Services;

namespace Frontenis.Application.Services;

/// <summary>Lectura de competiciones y cálculo de clasificaciones (por parejas o individual en ligas rotatorias, con ascensos y descensos).</summary>
public sealed class CompetitionQueries : ICompetitionQueries
{
    private readonly ICompetitionRepository _repository;
    private readonly IStandingsCalculator _calculator;
    private readonly IIndividualStandingsCalculator _individual;

    public CompetitionQueries(ICompetitionRepository repository, IStandingsCalculator calculator,
        IIndividualStandingsCalculator individual)
    {
        _repository = repository;
        _calculator = calculator;
        _individual = individual;
    }

    public async Task<IReadOnlyList<CompetitionSummaryDto>> ListAsync(CancellationToken ct = default) =>
        (await _repository.ListAsync(ct)).Select(c => c.ToSummary()).ToList();

    public async Task<CompetitionDetailDto> GetAsync(int id, CancellationToken ct = default) =>
        (await _repository.GetDetailedOrThrowAsync(id, ct)).ToDetailDto();

    public async Task<IReadOnlyList<GroupStandingsDto>> GetStandingsAsync(int id, CancellationToken ct = default)
    {
        var competition = await _repository.GetDetailedOrThrowAsync(id, ct);
        return competition.IsRotating ? RotatingStandings(competition) : GroupStandings(competition);
    }

    private IReadOnlyList<GroupStandingsDto> RotatingStandings(Competition competition)
    {
        var tables = competition.RotatingStandings(_individual);
        var movement = new Dictionary<int, int>();
        if (competition.Status == CompetitionStatus.Finished)
            foreach (var move in PromotionRules.Compute(tables.Select(t => t.Rows).ToList()))
                movement[move.PlayerId] = move.IsPromotion ? 1 : -1;

        return tables.Select(table =>
        {
            var group = competition.Groups.First(g => g.Id == table.GroupId);
            var names = competition.GroupPlayers(group).ToDictionary(p => p.Id, p => p.Name);
            var rows = table.Rows.Select(row => new StandingDto(
                row.Rank, row.EntryId, names[row.EntryId], row.Played, row.Won, row.Lost,
                row.PointsFor, row.PointsAgainst, row.PointsDiff, row.ClassificationPoints, false,
                movement.GetValueOrDefault(row.EntryId))).ToList();
            return new GroupStandingsDto(table.GroupId, table.GroupName, rows);
        }).ToList();
    }

    private IReadOnlyList<GroupStandingsDto> GroupStandings(Competition competition)
    {
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
}
