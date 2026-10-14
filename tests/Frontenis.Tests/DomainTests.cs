using System.Reflection;
using Frontenis.Domain.Entities;
using Frontenis.Domain.Enums;
using Frontenis.Domain.Exceptions;
using Frontenis.Domain.Models;
using Frontenis.Domain.Services;
using Xunit;

namespace Frontenis.Tests;

internal static class TestHelpers
{
    public static T WithId<T>(this T entity, int id) where T : class
    {
        typeof(T).GetProperty("Id")!.GetSetMethod(true)!.Invoke(entity, new object[] { id });
        return entity;
    }

    public static Player Player(int id, string name, PlayerPosition pos) => new Player(name, pos).WithId(id);
}

public class RandomPairingStrategyTests
{
    private static RandomPairingStrategy Strategy() => new(new SystemRandomizer(new Random(42)));

    [Fact]
    public void Balanced_players_always_make_back_plus_forward_pairs()
    {
        var players = new List<Player>();
        for (var i = 1; i <= 6; i++) players.Add(TestHelpers.Player(i, $"D{i}", PlayerPosition.Delantero));
        for (var i = 7; i <= 12; i++) players.Add(TestHelpers.Player(i, $"Z{i}", PlayerPosition.Zaguero));

        var result = Strategy().Generate(players);

        Assert.Equal(6, result.Pairs.Count);
        Assert.Null(result.Unpaired);
        Assert.All(result.Pairs, p => Assert.NotEqual(p.First.Position, p.Second.Position));
    }

    [Fact]
    public void Extra_forwards_are_paired_together_as_extreme_case()
    {
        var players = new List<Player>
        {
            TestHelpers.Player(1, "D1", PlayerPosition.Delantero), TestHelpers.Player(2, "D2", PlayerPosition.Delantero),
            TestHelpers.Player(3, "D3", PlayerPosition.Delantero), TestHelpers.Player(4, "D4", PlayerPosition.Delantero),
            TestHelpers.Player(5, "Z1", PlayerPosition.Zaguero), TestHelpers.Player(6, "Z2", PlayerPosition.Zaguero)
        };

        var result = Strategy().Generate(players);

        Assert.Equal(3, result.Pairs.Count);
        Assert.Equal(2, result.Pairs.Count(p => p.First.Position != p.Second.Position));
        Assert.Equal(1, result.Pairs.Count(p => p.First.Position == PlayerPosition.Delantero && p.Second.Position == PlayerPosition.Delantero));
    }

    [Fact]
    public void Odd_number_leaves_one_player_unpaired()
    {
        var players = new List<Player>
        {
            TestHelpers.Player(1, "D1", PlayerPosition.Delantero),
            TestHelpers.Player(2, "D2", PlayerPosition.Delantero),
            TestHelpers.Player(3, "Z1", PlayerPosition.Zaguero)
        };

        var result = Strategy().Generate(players);

        Assert.Single(result.Pairs);
        Assert.NotNull(result.Unpaired);
    }
}

public class RoundRobinScheduleGeneratorTests
{
    [Theory]
    [InlineData(4, false, 6)]
    [InlineData(5, false, 10)]
    [InlineData(4, true, 12)]
    public void Every_participant_meets_every_other(int n, bool doubleRound, int expected)
    {
        var ids = Enumerable.Range(1, n).ToList();
        var matches = new RoundRobinScheduleGenerator().Generate(ids, doubleRound);

        Assert.Equal(expected, matches.Count);
        var singles = matches.Take(n * (n - 1) / 2)
            .Select(m => (Math.Min(m.Home, m.Away), Math.Max(m.Home, m.Away))).Distinct().ToList();
        Assert.Equal(n * (n - 1) / 2, singles.Count);
    }

    [Fact]
    public void Nobody_plays_twice_in_the_same_round()
    {
        var matches = new RoundRobinScheduleGenerator().Generate(Enumerable.Range(1, 6).ToList(), false);
        foreach (var round in matches.GroupBy(m => m.Round))
        {
            var teams = round.SelectMany(m => new[] { m.Home, m.Away }).ToList();
            Assert.Equal(teams.Count, teams.Distinct().Count());
        }
    }
}

public class StandingsCalculatorTests
{
    [Fact]
    public void Orders_by_points_then_difference_then_points_for()
    {
        var outcomes = new[]
        {
            new MatchOutcome(1, 2, 15, 11),
            new MatchOutcome(2, 3, 15, 5),
            new MatchOutcome(3, 1, 15, 14)
        };

        var rows = new StandingsCalculator().Calculate(new[] { 1, 2, 3 }, outcomes, 3, 0);

        // Todos 1 victoria y 1 derrota (3 pts). Diferencias: 1 -> +3; 2 -> +6; 3 -> -9
        Assert.Equal(new[] { 2, 1, 3 }, rows.Select(r => r.EntryId).ToArray());
        Assert.All(rows, r => Assert.Equal(3, r.ClassificationPoints));
        Assert.Equal(1, rows[0].Rank);
    }

    [Fact]
    public void Configurable_points_per_win_and_loss()
    {
        var rows = new StandingsCalculator().Calculate(new[] { 1, 2 }, new[] { new MatchOutcome(1, 2, 15, 0) }, 2, 1);
        Assert.Equal(2, rows.Single(r => r.EntryId == 1).ClassificationPoints);
        Assert.Equal(1, rows.Single(r => r.EntryId == 2).ClassificationPoints);
    }
}

public class BracketGeneratorTests
{
    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(8)]
    public void Creates_participants_minus_one_matches(int participants)
    {
        var plan = new SingleEliminationBracketGenerator().Generate(participants);
        Assert.Equal(participants - 1, plan.Nodes.Count);
    }

    [Fact]
    public void Every_seed_is_placed_exactly_once()
    {
        var plan = new SingleEliminationBracketGenerator().Generate(6);
        var seeds = plan.Placements.Select(p => p.Seed).OrderBy(s => s).ToList();
        Assert.Equal(Enumerable.Range(1, 6), seeds);
    }
}

public class CompetitionTests
{
    private static Competition Build(int pairs = 4, int groups = 1, CompetitionType type = CompetitionType.League, int perGroup = 0, int qualifiers = 0)
    {
        var c = Competition.Create(new CompetitionSettings("Test", type, pairs, groups, perGroup, 3, 0, false, qualifiers));
        for (var i = 1; i <= pairs; i++)
        {
            var pair = new Pair(TestHelpers.Player(i * 2, $"Z{i}", PlayerPosition.Zaguero), TestHelpers.Player(i * 2 + 1, $"D{i}", PlayerPosition.Delantero));
            c.AddPair(pair).WithId(i);
        }
        return c;
    }

    [Fact]
    public void Rejects_inconsistent_group_parameters()
    {
        Assert.Throws<DomainException>(() =>
            Competition.Create(new CompetitionSettings("X", CompetitionType.League, 8, 2, 3, 3, 0, false, 0)));
    }

    [Fact]
    public void Rejects_groups_with_less_than_two_pairs()
    {
        Assert.Throws<DomainException>(() =>
            Competition.Create(new CompetitionSettings("X", CompetitionType.League, 3, 2, 0, 3, 0, false, 0)));
    }

    [Fact]
    public void Draw_creates_groups_and_matches()
    {
        var c = Build(pairs: 8, groups: 2);
        c.Draw(new ShuffleGroupDistributor(new SystemRandomizer(new Random(1))), new RoundRobinScheduleGenerator());

        Assert.Equal(CompetitionStatus.GroupStage, c.Status);
        Assert.Equal(2, c.Groups.Count);
        Assert.Equal(12, c.Matches.Count); // 2 grupos x 6 partidos
    }

    [Fact]
    public void Tie_is_not_allowed_and_league_finishes_when_all_played()
    {
        var c = Build(pairs: 2);
        c.Draw(new ShuffleGroupDistributor(new SystemRandomizer()), new RoundRobinScheduleGenerator());
        var match = c.Matches.Single().WithId(1);

        Assert.Throws<DomainException>(() => c.RecordResult(1, 15, 15));
        c.RecordResult(1, 15, 11);

        Assert.Equal(CompetitionStatus.Finished, c.Status);
        Assert.Equal(match.HomeEntry, match.Winner);
    }
}
