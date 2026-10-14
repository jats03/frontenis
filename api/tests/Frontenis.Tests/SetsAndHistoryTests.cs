using Frontenis.Domain.Entities;
using Frontenis.Domain.Enums;
using Frontenis.Domain.Exceptions;
using Frontenis.Domain.Models;
using Frontenis.Domain.Services;
using Xunit;

namespace Frontenis.Tests;

public class SetsPerMatchTests
{
    private static Competition TwoPairs(int sets)
    {
        var c = Competition.Create(new CompetitionSettings("T", CompetitionType.League, 2, 1, 0, 3, 1, false, 0, 0, sets));
        for (var i = 1; i <= 2; i++)
            c.AddPair(new Pair(TestHelpers.Player(i * 2, $"Z{i}", PlayerPosition.Zaguero),
                TestHelpers.Player(i * 2 + 1, $"D{i}", PlayerPosition.Delantero))).WithId(i);
        c.Draw(new ShuffleGroupDistributor(new SystemRandomizer()), new RoundRobinScheduleGenerator());
        c.Matches.Single().WithId(1);
        return c;
    }

    private static IReadOnlyList<SetScore> S(params (int h, int a)[] sets) => sets.Select(s => new SetScore(s.h, s.a)).ToList();

    [Fact]
    public void Sets_per_match_must_be_one_or_two()
    {
        Assert.Throws<DomainException>(() => Competition.Create(
            new CompetitionSettings("T", CompetitionType.League, 2, 1, 0, 3, 1, false, 0, 0, 3)));
        Assert.Throws<DomainException>(() => Competition.Create(
            new CompetitionSettings("T", CompetitionType.League, 2, 1, 0, 3, 1, false, 0, 0, 0)));
    }

    [Fact]
    public void One_set_match_needs_exactly_one_set_and_no_tie()
    {
        var c = TwoPairs(1);
        Assert.Throws<DomainException>(() => c.RecordResult(1, S((6, 4), (6, 3))));
        Assert.Throws<DomainException>(() => c.RecordResult(1, S((5, 5))));
        c.RecordResult(1, S((6, 4)));
        Assert.Equal(CompetitionStatus.Finished, c.Status);
    }

    [Fact]
    public void Two_sets_two_zero_needs_no_third_set()
    {
        var c = TwoPairs(2);
        Assert.Throws<DomainException>(() => c.RecordResult(1, S((6, 4), (6, 3), (6, 1))));
        var m = c.RecordResult(1, S((6, 4), (6, 3)));
        Assert.Equal(2, m.HomeSets);
        Assert.Equal(0, m.AwaySets);
        Assert.Equal(12, m.HomeScore);
        Assert.Equal(7, m.AwayScore);
        Assert.Equal(m.HomeEntry, m.Winner);
    }

    [Fact]
    public void Two_sets_split_requires_third_set_and_it_decides()
    {
        var c = TwoPairs(2);
        Assert.Throws<DomainException>(() => c.RecordResult(1, S((6, 4), (3, 6))));
        Assert.Throws<DomainException>(() => c.RecordResult(1, S((6, 4))));
        var m = c.RecordResult(1, S((6, 4), (3, 6), (4, 6)));
        Assert.Equal(1, m.HomeSets);
        Assert.Equal(2, m.AwaySets);
        Assert.Equal(m.AwayEntry, m.Winner);
        Assert.Equal(13, m.HomeScore); // los tantos totales pueden favorecer al perdedor
    }

    [Fact]
    public void Winner_by_sets_even_when_total_points_are_lower()
    {
        var c = TwoPairs(2);
        var m = c.RecordResult(1, S((6, 0), (4, 6), (4, 6)));
        Assert.Equal(m.AwayEntry, m.Winner);
        Assert.True(m.HomeScore > m.AwayScore);
    }

    [Fact]
    public void Negative_or_tied_sets_are_rejected_and_result_can_be_cleared()
    {
        var c = TwoPairs(2);
        Assert.Throws<DomainException>(() => c.RecordResult(1, S((-1, 4), (6, 3))));
        Assert.Throws<DomainException>(() => c.RecordResult(1, S((4, 4), (6, 3))));
        c.RecordResult(1, S((6, 4), (6, 3)));
        c.ClearResult(1);
        Assert.False(c.Matches.Single().IsPlayed);
        Assert.Empty(c.Matches.Single().Sets);
    }

    [Fact]
    public void Format_can_change_only_before_any_result()
    {
        var c = TwoPairs(1);
        c.ChangeSetsPerMatch(2);
        Assert.Equal(2, c.SetsPerMatch);
        c.RecordResult(1, S((6, 4), (6, 3)));
        Assert.Throws<DomainException>(() => c.ChangeSetsPerMatch(1));
    }

    [Fact]
    public void Legacy_two_number_result_is_a_single_set()
    {
        var c = TwoPairs(1);
        var m = c.RecordResult(1, 15, 11);
        Assert.Equal(1, m.HomeSets);
        Assert.Equal(15, m.HomeScore);
    }

    [Fact]
    public void Standings_use_sets_to_decide_the_winner()
    {
        var o = new MatchOutcome(1, 2, 14, 12, 1, 2); // más tantos pero pierde por sets
        Assert.False(o.HomeWon);
        var rows = new StandingsCalculator().Calculate(new[] { 1, 2 }, new[] { o }, 3, 1);
        Assert.Equal(2, rows.First().EntryId);
    }
}

public class PlayerStatsTests
{
    private static PlayedMatch M(int id, int comp, bool group, int[] home, int[] away, params (int h, int a)[] sets) =>
        new(id, comp, $"C{comp}", group, 3, 1, home, away, sets.Select(s => new SetScore(s.h, s.a)).ToList());

    [Fact]
    public void Wins_losses_points_partners_and_rivals_are_counted()
    {
        var matches = new List<PlayedMatch>
        {
            M(1, 1, true, new[] { 1, 2 }, new[] { 3, 4 }, (6, 4), (6, 3)),          // 1 y 2 ganan
            M(2, 1, true, new[] { 1, 3 }, new[] { 2, 4 }, (4, 6), (6, 4), (3, 6)),  // 1 y 3 pierden
            M(3, 2, false, new[] { 1, 2 }, new[] { 3, 4 }, (6, 1))                  // eliminatoria: no suma puntos
        };
        var calc = new PlayerStatsCalculator();
        var p1 = calc.Calculate(1, matches);

        Assert.Equal(3, p1.Played);
        Assert.Equal(2, p1.Won);
        Assert.Equal(1, p1.Lost);
        Assert.Equal(4, p1.ClassificationPoints); // 3 (victoria) + 1 (derrota); la eliminatoria no cuenta
        Assert.Equal(2, p1.Competitions);
        Assert.Equal(4, p1.SetsFor);
        Assert.Equal(2, p1.SetsAgainst);

        var withTwo = p1.Partners.Single(r => r.PlayerId == 2);
        Assert.Equal(2, withTwo.Played);
        Assert.Equal(2, withTwo.Won);
        var withThree = p1.Partners.Single(r => r.PlayerId == 3);
        Assert.Equal(1, withThree.Lost);
        Assert.Equal(2, p1.Partners.First().Played); // el más frecuente primero
        Assert.Equal(3, p1.Rivals.Single(r => r.PlayerId == 4).Played);
    }

    [Fact]
    public void Calculate_all_ranks_by_points_and_unknown_player_is_empty()
    {
        var matches = new List<PlayedMatch> { M(1, 1, true, new[] { 1, 2 }, new[] { 3, 4 }, (6, 4)) };
        var calc = new PlayerStatsCalculator();
        var all = calc.CalculateAll(matches);
        Assert.Equal(4, all.Count);
        Assert.True(all.First().ClassificationPoints == 3);
        Assert.Equal(0, calc.Calculate(99, matches).Played);
    }

    [Fact]
    public void Competition_and_training_with_same_id_are_different_sources()
    {
        var sets = new[] { new SetScore(15, 10) };
        var matches = new List<PlayedMatch>
        {
            new(1, 1, "Liga", true, 3, 0, new[] { 1, 2 }, new[] { 3, 4 }, sets),
            new(1, 1, "Martes", true, 2, 1, new[] { 1, 2 }, new[] { 3, 4 }, sets, true),
        };
        var p1 = new PlayerStatsCalculator().Calculate(1, matches);
        Assert.Equal(2, p1.Competitions);
        Assert.Equal(5, p1.ClassificationPoints); // 3 de la liga + 2 del entrenamiento
    }

    [Fact]
    public void History_filter_semantics()
    {
        Assert.True(Frontenis.Application.Models.HistoryFilter.All.IsAll);
        var onlyTr = new Frontenis.Application.Models.HistoryFilter(new int[0], new[] { 4 });
        Assert.False(onlyTr.IncludesCompetitions);
        Assert.True(onlyTr.IncludesTrainings);
    }
}
