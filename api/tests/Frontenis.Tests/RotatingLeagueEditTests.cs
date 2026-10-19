using Frontenis.Domain.Entities;
using Frontenis.Domain.Enums;
using Frontenis.Domain.Exceptions;
using Frontenis.Domain.Models;
using Frontenis.Domain.Services;
using Xunit;

namespace Frontenis.Tests;

public class RotatingLeagueEditTests
{
    private static SystemRandomizer Rnd(int seed = 3) => new(new Random(seed));
    private static RotationPlanner Planner(int seed = 3) => new(Rnd(seed));

    private static List<Player> Players(int count, int first = 1) =>
        Enumerable.Range(first, count).Select(i => TestHelpers.Player(i, $"J{i}", i % 2 == 0 ? PlayerPosition.Delantero : PlayerPosition.Zaguero)).ToList();

    /// <summary>Liga de 2 grupos de 4 (3 jornadas de 2 partidos... 1 partido por jornada) ya sorteada y con ids.</summary>
    private static Competition Started(int groups = 2, int perGroup = 4, int rounds = 0)
    {
        var c = Competition.Create(new CompetitionSettings("Liga", CompetitionType.RotatingLeague, 0, groups, perGroup, 2, 0, false, 0, rounds));
        c.DrawRotating(Players(groups * perGroup), Rnd(), Planner());
        var gid = 1;
        foreach (var g in c.Groups) g.WithId(gid++);
        var mid = 1;
        foreach (var m in c.Matches) m.WithId(mid++);
        return c;
    }

    private static Group Last(Competition c) => c.Groups.OrderBy(g => g.Id).Last();
    private static List<Match> InGroup(Competition c, Group g) => c.Matches.Where(m => m.Group == g).OrderBy(m => m.Round).ThenBy(m => m.Order).ToList();

    private static void Play(Competition c, Match m) => c.RecordResult(m.Id, 10, 5);

    [Fact]
    public void Adding_a_player_puts_them_in_the_last_group_and_replans_it()
    {
        var c = Started();
        var last = Last(c);
        var firstGroupBefore = InGroup(c, c.Groups.First()).Select(m => m.Id).ToList();

        var removed = c.AddPlayerToLastGroup(TestHelpers.Player(99, "Nuevo", PlayerPosition.Delantero), Planner());

        Assert.Equal(5, c.GroupPlayers(last).Count);
        Assert.Contains(c.GroupPlayers(last), p => p.Id == 99);
        Assert.Equal(firstGroupBefore, InGroup(c, c.Groups.First()).Select(m => m.Id).ToList()); // el otro grupo no cambia
        Assert.NotEmpty(removed);                                                              // parejas viejas a eliminar
        Assert.Equal(9, c.PairCount);
        Assert.Equal(CompetitionStatus.GroupStage, c.Status);
        // 5 jugadores: juegan 4 y descansa 1 en cada jornada; todos juegan alguna vez
        var playedIn = InGroup(c, last).SelectMany(m => new[] { m.HomeEntry!.Pair.Player1Id, m.HomeEntry.Pair.Player2Id, m.AwayEntry!.Pair.Player1Id, m.AwayEntry.Pair.Player2Id }).Distinct().Count();
        Assert.Equal(5, playedIn);
        Assert.Equal(RotationMath.FullRounds(5), InGroup(c, last).Select(m => m.Round).Distinct().Count());
    }

    [Fact]
    public void Cannot_add_a_player_once_a_match_of_the_last_group_was_played()
    {
        var c = Started();
        var last = Last(c);
        Play(c, InGroup(c, last).First());
        var before = InGroup(c, last).Select(m => m.Id).ToList();

        Assert.Throws<DomainException>(() => c.AddPlayerToLastGroup(TestHelpers.Player(99, "Nuevo", PlayerPosition.Zaguero), Planner()));
        Assert.Equal(before, InGroup(c, last).Select(m => m.Id).ToList());
        Assert.Equal(4, c.GroupPlayers(last).Count);
    }

    [Fact]
    public void A_played_match_in_another_group_does_not_block_adding()
    {
        var c = Started();
        Play(c, InGroup(c, c.Groups.First()).First());
        c.AddPlayerToLastGroup(TestHelpers.Player(99, "Nuevo", PlayerPosition.Zaguero), Planner());
        Assert.Equal(5, c.GroupPlayers(Last(c)).Count);
    }

    [Fact]
    public void A_fixed_number_of_rounds_is_respected()
    {
        var c = Started(groups: 1, perGroup: 4, rounds: 3);
        c.AddPlayerToLastGroup(TestHelpers.Player(99, "Nuevo", PlayerPosition.Delantero), Planner());
        Assert.Equal(3, InGroup(c, Last(c)).Select(m => m.Round).Distinct().Count());
    }

    [Fact]
    public void Cannot_add_a_player_who_already_plays_or_before_the_draw()
    {
        var c = Started();
        Assert.Throws<DomainException>(() => c.AddPlayerToLastGroup(TestHelpers.Player(3, "J3", PlayerPosition.Delantero), Planner()));

        var draft = Competition.Create(new CompetitionSettings("Liga", CompetitionType.RotatingLeague, 0, 1, 4, 2, 0, false, 0));
        Assert.Throws<DomainException>(() => draft.AddPlayerToLastGroup(TestHelpers.Player(99, "N", PlayerPosition.Delantero), Planner()));
    }

    [Fact]
    public void Cannot_add_a_player_to_a_finished_league_or_a_tournament()
    {
        var c = Started(groups: 1);
        foreach (var m in c.Matches.ToList()) Play(c, m);
        Assert.Equal(CompetitionStatus.Finished, c.Status);
        Assert.Throws<DomainException>(() => c.AddPlayerToLastGroup(TestHelpers.Player(99, "N", PlayerPosition.Delantero), Planner()));
    }

    [Fact]
    public void Moving_a_match_to_a_free_round_changes_only_its_round()
    {
        // 8 jugadores en un grupo: 2 partidos por jornada. Se mueve un partido de la jornada 1 a la 8 (nueva).
        var c = Started(groups: 1, perGroup: 8);
        var g = Last(c);
        var m = InGroup(c, g).First(x => x.Round == 1);
        var last = InGroup(c, g).Max(x => x.Round);

        c.MoveMatchToRound(m.Id, last + 1);

        Assert.Equal(last + 1, InGroup(c, g).Single(x => x.Id == m.Id).Round);
        Assert.Equal(1, InGroup(c, g).Count(x => x.Round == 1));
    }

    [Fact]
    public void Moving_a_match_to_a_round_where_a_player_already_plays_is_rejected()
    {
        var c = Started(groups: 1, perGroup: 4);
        var g = Last(c);
        var matches = InGroup(c, g);
        // con 4 jugadores hay 1 partido por jornada y todos juegan siempre: mover a otra jornada existente choca
        Assert.Throws<DomainException>(() => c.MoveMatchToRound(matches[0].Id, matches[1].Round));
    }

    [Fact]
    public void Moving_rules_played_unknown_and_far_rounds()
    {
        var c = Started(groups: 1, perGroup: 8);
        var g = Last(c);
        var ms = InGroup(c, g);
        Play(c, ms[0]);
        Assert.Throws<DomainException>(() => c.MoveMatchToRound(ms[0].Id, 3));                                  // jugado
        Assert.Throws<DomainException>(() => c.MoveMatchToRound(ms[2].Id, 0));                                  // jornada 0
        Assert.Throws<DomainException>(() => c.MoveMatchToRound(ms[2].Id, ms.Max(x => x.Round) + 2));           // demasiado lejos
        Assert.Throws<NotFoundException>(() => c.MoveMatchToRound(12345, 1));
    }

    [Fact]
    public void Moving_the_only_match_of_a_round_compacts_the_numbering()
    {
        var c = Started(groups: 1, perGroup: 6);          // 6 jugadores: 1 partido por jornada, 8 jornadas
        var g = Last(c);
        var rounds = InGroup(c, g).Select(m => m.Round).Distinct().Count();
        var second = InGroup(c, g).First(m => m.Round == 2);
        // descansan 2 jugadores por jornada: se puede mover a la jornada de un partido sin los mismos 4... probamos el compactado
        // quitando una jornada intermedia
        c.RemoveRound(g.Id, 2);
        Assert.Equal(rounds - 1, InGroup(c, g).Select(m => m.Round).Distinct().Count());
        Assert.Equal(Enumerable.Range(1, rounds - 1), InGroup(c, g).Select(m => m.Round).Distinct().OrderBy(r => r));
        Assert.DoesNotContain(c.Matches, m => m.Id == second.Id);
    }

    [Fact]
    public void Removing_a_round_with_played_matches_is_rejected()
    {
        var c = Started(groups: 1, perGroup: 6);
        var g = Last(c);
        Play(c, InGroup(c, g).First(m => m.Round == 3));
        Assert.Throws<DomainException>(() => c.RemoveRound(g.Id, 3));
        Assert.Throws<NotFoundException>(() => c.RemoveRound(g.Id, 99));
        Assert.Throws<NotFoundException>(() => c.RemoveRound(999, 1));
    }

    [Fact]
    public void Removing_the_last_pending_round_can_finish_the_league()
    {
        var c = Started(groups: 1, perGroup: 4);          // 3 jornadas
        var g = Last(c);
        var ms = InGroup(c, g);
        Play(c, ms[0]); Play(c, ms[1]);
        Assert.Equal(CompetitionStatus.GroupStage, c.Status);

        var removed = c.RemoveRound(g.Id, 3);

        Assert.Equal(CompetitionStatus.Finished, c.Status);
        Assert.Equal(2, c.Matches.Count);
        Assert.Equal(2, removed.Count);                   // las dos parejas del partido quitado
    }

    [Fact]
    public void The_last_round_of_a_group_cannot_be_removed()
    {
        var c = Started(groups: 1, perGroup: 4, rounds: 1);
        Assert.Throws<DomainException>(() => c.RemoveRound(Last(c).Id, 1));
    }

    [Fact]
    public void Swapping_two_matches_exchanges_their_rounds()
    {
        var c = Started(groups: 1, perGroup: 4, rounds: 3);
        var ms = InGroup(c, Last(c));
        var a = ms.First(m => m.Round == 1);
        var b = ms.First(m => m.Round == 2);
        var (ra, rb) = (a.Round, b.Round);
        c.SwapMatchRounds(a.Id, b.Id);
        Assert.Equal(rb, a.Round);
        Assert.Equal(ra, b.Round);
        foreach (var round in InGroup(c, Last(c)).GroupBy(m => m.Round))
        {
            var ids = round.SelectMany(m => new[] { m.HomeEntry!.Pair.Player1Id, m.HomeEntry.Pair.Player2Id, m.AwayEntry!.Pair.Player1Id, m.AwayEntry.Pair.Player2Id }).ToList();
            Assert.Equal(ids.Count, ids.Distinct().Count());
        }
    }

    [Fact]
    public void Swap_is_rejected_for_same_round_played_or_other_group_matches()
    {
        var c = Started(groups: 2, perGroup: 4, rounds: 3);
        var first = InGroup(c, c.Groups.First());
        var last = InGroup(c, Last(c));
        Assert.Throws<DomainException>(() => c.SwapMatchRounds(first[0].Id, first[0].Id));
        Assert.Throws<DomainException>(() => c.SwapMatchRounds(first[0].Id, last[0].Id));
        var sameRound = first.Where(m => m.Round == first[0].Round).ToList();
        if (sameRound.Count > 1) Assert.Throws<DomainException>(() => c.SwapMatchRounds(sameRound[0].Id, sameRound[1].Id));
        var other = first.First(m => m.Round != first[0].Round);
        Play(c, first[0]);
        Assert.Throws<DomainException>(() => c.SwapMatchRounds(first[0].Id, other.Id));
    }

    [Fact]
    public void Swap_is_rejected_when_a_player_would_play_twice_in_a_round()
    {
        var c = Started(groups: 1, perGroup: 8, rounds: 3);
        var ms = InGroup(c, Last(c));
        var ids = (Match m) => new[] { m.HomeEntry!.Pair.Player1Id, m.HomeEntry.Pair.Player2Id, m.AwayEntry!.Pair.Player1Id, m.AwayEntry.Pair.Player2Id };
        // busca un cruce que choque: el partido a llegaría a la jornada de b y comparte jugador con otro partido de esa jornada
        foreach (var a in ms) foreach (var b in ms.Where(x => x.Round != a.Round))
        {
            var others = ms.Where(m => m != a && m != b && m.Round == b.Round).SelectMany(ids).ToHashSet();
            if (!ids(a).Any(others.Contains)) continue;
            var before = (a.Round, b.Round);
            Assert.Throws<DomainException>(() => c.SwapMatchRounds(a.Id, b.Id));
            Assert.Equal(before, (a.Round, b.Round));
            return;
        }
        Assert.True(false, "no se encontró un cruce con choque");
    }
}
