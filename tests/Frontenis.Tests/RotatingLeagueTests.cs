using Frontenis.Domain.Entities;
using Frontenis.Domain.Enums;
using Frontenis.Domain.Exceptions;
using Frontenis.Domain.Services;
using Xunit;

namespace Frontenis.Tests;

public class RotatingLeagueTests
{
    private static SystemRandomizer Rnd(int seed = 7) => new(new Random(seed));

    private static List<Player> Players(int count)
    {
        var list = new List<Player>();
        for (var i = 1; i <= count; i++)
            list.Add(TestHelpers.Player(i, $"J{i}", i <= count / 2 ? PlayerPosition.Delantero : PlayerPosition.Zaguero));
        return list;
    }

    private static Competition NewLeague(int groups, int perGroup) =>
        Competition.Create(new CompetitionSettings("Liga", CompetitionType.RotatingLeague, 0, groups, perGroup, 2, 0, false, 0));

    private static void NumberMatches(Competition c)
    {
        var id = 1;
        foreach (var m in c.Matches) m.WithId(id++);
    }

    [Fact]
    public void Planner_never_repeats_a_partner_when_rounds_fit()
    {
        var players = Players(8);
        var rounds = new RotationPlanner(Rnd()).Plan(players, 4);

        var seen = new HashSet<(int, int)>();
        foreach (var round in rounds)
            foreach (var m in round.Matches)
                foreach (var (a, b) in new[] { m.Home, m.Away })
                    Assert.True(seen.Add((Math.Min(a.Id, b.Id), Math.Max(a.Id, b.Id))), "compañero repetido");
    }

    [Fact]
    public void Planner_uses_every_player_exactly_once_per_round()
    {
        var players = Players(12);
        foreach (var round in new RotationPlanner(Rnd()).Plan(players, 5))
        {
            var ids = round.Matches.SelectMany(m => new[] { m.Home.First.Id, m.Home.Second.Id, m.Away.First.Id, m.Away.Second.Id }).ToList();
            Assert.Equal(12, ids.Count);
            Assert.Equal(12, ids.Distinct().Count());
        }
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public void Group_size_must_be_at_least_four(int size)
    {
        Assert.Throws<DomainException>(() => NewLeague(1, size));
    }

    [Fact]
    public void Draw_creates_balanced_groups_and_matches()
    {
        var c = NewLeague(2, 8);
        c.DrawRotating(Players(16), Rnd(), new RotationPlanner(Rnd()));

        Assert.Equal(CompetitionStatus.GroupStage, c.Status);
        Assert.Equal(2, c.Groups.Count);
        Assert.All(c.Groups, g => Assert.Equal(8, c.GroupPlayers(g).Count));
        Assert.Equal(2 * 4 * 2, c.Matches.Count); // 2 grupos x 4 jornadas x 2 partidos
    }

    [Fact]
    public void Draw_rejects_too_few_players()
    {
        var c = NewLeague(2, 8);
        Assert.Throws<DomainException>(() => c.DrawRotating(Players(12), Rnd(), new RotationPlanner(Rnd())));
    }

    [Fact]
    public void Extra_players_go_to_the_last_group_and_rest_in_turns()
    {
        var c = NewLeague(4, 4);
        c.DrawRotating(Players(18), Rnd(), new RotationPlanner(Rnd()));

        var sizes = c.Groups.Select(g => c.GroupPlayers(g).Count).ToList();
        Assert.Equal(4, sizes[0]);
        Assert.Equal(4, sizes[2]);
        Assert.Equal(6, sizes[3]);
        Assert.Equal(18, c.PairCount);

        // En el grupo de 6, cada jornada juegan 4 y descansan 2; nadie descansa dos jornadas seguidas mientras otros no han descansado.
        var last = c.Groups.Last();
        var perRound = c.Matches.Where(m => m.Group == last).GroupBy(m => m.Round).ToList();
        Assert.All(perRound, r => Assert.Equal(1, r.Count()));
        var played = c.Matches.Where(m => m.Group == last)
            .SelectMany(m => new[] { m.HomeEntry!.Pair.Player1Id, m.HomeEntry.Pair.Player2Id, m.AwayEntry!.Pair.Player1Id, m.AwayEntry.Pair.Player2Id })
            .GroupBy(x => x).Select(g => g.Count()).ToList();
        Assert.Equal(6, played.Count);   // todos juegan alguna jornada
    }

    [Fact]
    public void Registering_fixed_pairs_is_not_allowed()
    {
        var c = NewLeague(1, 4);
        var p = Players(4);
        Assert.Throws<DomainException>(() => c.AddPair(new Pair(p[0], p[3])));
    }

    [Fact]
    public void Finishing_all_matches_finishes_league_and_promotions_apply()
    {
        var c = NewLeague(2, 8);
        var planner = new RotationPlanner(Rnd());
        c.DrawRotating(Players(16), Rnd(), planner);
        NumberMatches(c);

        var rnd = new Random(3);
        foreach (var m in c.Matches.ToList())
        {
            var win = rnd.Next(2) == 0;
            var other = rnd.Next(0, 9);
            c.RecordResult(m.Id, win ? 10 : other, win ? other : 10);
        }
        Assert.Equal(CompetitionStatus.Finished, c.Status);

        var calc = new IndividualStandingsCalculator();
        var tables = c.Groups.Select(g => calc.Calculate(
            c.GroupPlayers(g).Select(p => p.Id).ToList(), c.GroupTeamOutcomes(g), 2, 0)).ToList();
        var moves = PromotionRules.Compute(tables.Select(t => (IReadOnlyList<StandingRow>)t).ToList());

        Assert.Equal(2, moves.Count);
        Assert.Contains(moves, m => m.IsPromotion && m.PlayerId == tables[1][0].EntryId);
        Assert.Contains(moves, m => !m.IsPromotion && m.PlayerId == tables[0][^1].EntryId);

        var rosters = c.Groups.Select(g => (IReadOnlyList<int>)c.GroupPlayers(g).Select(p => p.Id).ToList()).ToList();
        var next = PromotionRules.NextRosters(rosters, moves);
        Assert.All(next, r => Assert.Equal(8, r.Count));
    }

    [Fact]
    public void Next_league_name_increments()
    {
        var c = NewLeague(1, 4);
        c.DrawRotating(Players(4), Rnd(), new RotationPlanner(Rnd()));
        NumberMatches(c);
        foreach (var m in c.Matches.ToList()) c.RecordResult(m.Id, 10, 3);

        var roster = new List<IReadOnlyList<Player>> { Players(4) };
        var next = Competition.CreateNext(c, roster, new RotationPlanner(Rnd()));
        Assert.Equal("Liga (2)", next.Name);
        Assert.Equal(CompetitionStatus.GroupStage, next.Status);
    }
}
