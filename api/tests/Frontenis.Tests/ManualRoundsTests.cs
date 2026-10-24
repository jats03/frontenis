using Frontenis.Domain.Entities;
using Frontenis.Domain.Enums;
using Frontenis.Domain.Exceptions;
using Frontenis.Domain.Models;
using Frontenis.Domain.Services;
using Xunit;

namespace Frontenis.Tests;

public class ManualRoundsTests
{
    private static SystemRandomizer Rnd() => new(new Random(3));
    private static RotationPlanner Planner() => new(Rnd());
    private static List<Player> Players(int count, int first = 1) =>
        Enumerable.Range(first, count).Select(i => TestHelpers.Player(i, $"J{i}", i % 2 == 0 ? PlayerPosition.Delantero : PlayerPosition.Zaguero)).ToList();

    private static Competition Manual(int groups = 2, int perGroup = 4)
    {
        var c = Competition.Create(new CompetitionSettings("Liga", CompetitionType.RotatingLeague, 0, groups, perGroup, 2, 0, false, 0, 0, 1, true));
        var rosters = Enumerable.Range(0, groups).Select(g => (IReadOnlyList<Player>)Players(perGroup, g * perGroup + 1)).ToList();
        c.DrawRotatingManual(rosters, Planner());
        var gid = 1; foreach (var g in c.Groups) g.WithId(gid++);
        return c;
    }

    [Fact]
    public void Manual_draw_creates_groups_with_their_players_but_no_matches()
    {
        var c = Manual();
        Assert.True(c.ManualRounds);
        Assert.Empty(c.Matches);
        Assert.Empty(c.Entries);
        Assert.Equal(2, c.Groups.Count);
        Assert.Equal(4, c.GroupPlayers(c.Groups.First()).Count);
        Assert.Equal(CompetitionStatus.GroupStage, c.Status);   // sin partidos no está «terminada»
    }

    [Fact]
    public void Rounds_are_created_by_adding_matches_in_order()
    {
        var c = Manual();
        var g = c.Groups.First();
        c.AddMatchToRound(g.Id, 1, 1, 2, 3, 4);
        c.AddMatchToRound(g.Id, 2, 1, 3, 2, 4);
        Assert.Equal(new[] { 1, 2 }, c.Matches.Select(m => m.Round).OrderBy(r => r));
        Assert.Throws<DomainException>(() => c.AddMatchToRound(g.Id, 4, 1, 4, 2, 3));     // no se saltan jornadas
        Assert.Throws<DomainException>(() => c.AddMatchToRound(g.Id, 1, 1, 2, 3, 5));     // jugador de otro grupo
        Assert.Equal(CompetitionStatus.GroupStage, c.Status);
    }

    [Fact]
    public void Group_finishes_only_when_it_has_matches_and_all_are_played()
    {
        var c = Manual(1);
        var g = c.Groups.Single();
        var m = c.AddMatchToRound(g.Id, 1, 1, 2, 3, 4);
        m.WithId(1);
        c.RecordResult(1, 10, 5);
        Assert.Equal(CompetitionStatus.Finished, c.Status);
    }

    [Fact]
    public void Adding_a_player_to_a_manual_league_only_assigns_them_to_the_last_group()
    {
        var c = Manual();
        var removed = c.AddPlayerToLastGroup(TestHelpers.Player(99, "Nuevo", PlayerPosition.Delantero), Planner());
        Assert.Empty(removed);
        Assert.Empty(c.Matches);
        Assert.Contains(c.GroupPlayers(c.Groups.Last()), p => p.Id == 99);
        Assert.Throws<DomainException>(() => c.AddPlayerToLastGroup(TestHelpers.Player(99, "Nuevo", PlayerPosition.Delantero), Planner()));
    }

    [Fact]
    public void Automatic_leagues_are_unchanged()
    {
        var c = Competition.Create(new CompetitionSettings("Liga", CompetitionType.RotatingLeague, 0, 1, 4, 2, 0, false, 0));
        c.DrawRotating(Players(4), Rnd(), Planner());
        Assert.False(c.ManualRounds);
        Assert.NotEmpty(c.Matches);
    }

    [Fact]
    public void Removing_a_player_from_a_manual_group_keeps_at_least_four()
    {
        var c = Manual(1, 5);
        var g = c.Groups.Single();
        c.RemovePlayerFromGroup(g.Id, 5);
        Assert.Equal(4, c.GroupPlayers(g).Count);
        Assert.Throws<DomainException>(() => c.RemovePlayerFromGroup(g.Id, 4));   // quedarían 3
        Assert.Throws<DomainException>(() => c.RemovePlayerFromGroup(g.Id, 5));   // ya no está
    }

    [Fact]
    public void A_player_who_has_played_cannot_be_removed_but_one_without_matches_played_can()
    {
        var c = Manual(1, 6);
        var g = c.Groups.Single();
        var m1 = c.AddMatchToRound(g.Id, 1, 1, 2, 3, 4); m1.WithId(1);
        var m2 = c.AddMatchToRound(g.Id, 2, 5, 6, 1, 2); m2.WithId(2);
        c.RecordResult(1, 10, 5);
        Assert.Throws<DomainException>(() => c.RemovePlayerFromGroup(g.Id, 1));   // jugó el partido 1
        var removed = c.RemovePlayerFromGroup(g.Id, 6);                            // solo estaba en el partido pendiente 2
        Assert.Equal(2, removed.Count);
        Assert.Single(c.Matches);
        Assert.DoesNotContain(c.GroupPlayers(g), p => p.Id == 6);
    }

    [Fact]
    public void Removing_a_player_from_an_automatic_league_drops_only_their_pending_matches()
    {
        var c = Competition.Create(new CompetitionSettings("Liga", CompetitionType.RotatingLeague, 0, 1, 4, 2, 0, false, 0));
        c.DrawRotating(Players(5), Rnd(), Planner());
        var gid = 1; foreach (var gr in c.Groups) gr.WithId(gid++);
        var mid = 1; foreach (var m in c.Matches) m.WithId(mid++);
        var before = c.Matches.Count;
        c.RemovePlayerFromGroup(1, 5);
        Assert.True(c.Matches.Count < before);
        Assert.DoesNotContain(c.Matches, m => m.HomeEntry!.Pair.Contains(5) || m.AwayEntry!.Pair.Contains(5));
        Assert.Equal(4, c.GroupPlayers(c.Groups.Single()).Count);
    }
}
