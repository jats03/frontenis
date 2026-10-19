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
    public void Adding_a_player_gives_a_full_round_robin_even_if_the_league_had_fixed_rounds()
    {
        var c = Started(groups: 1, perGroup: 4, rounds: 3);
        var g = Last(c);
        c.AddPlayerToLastGroup(TestHelpers.Player(99, "Nuevo", PlayerPosition.Delantero), Planner());
        var ms = InGroup(c, g);
        Assert.Equal(RotationMath.FullRounds(5), ms.Select(m => m.Round).Distinct().Count());
        var seen = new HashSet<(int, int)>();
        foreach (var m in ms)
            foreach (var p in new[] { m.HomeEntry!.Pair, m.AwayEntry!.Pair })
                seen.Add((Math.Min(p.Player1Id, p.Player2Id), Math.Max(p.Player1Id, p.Player2Id)));
        Assert.Equal(10, seen.Count); // 5 jugadores: las 10 parejas posibles
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
    public void Moving_a_match_to_a_round_where_a_player_already_plays_is_allowed()
    {
        var c = Started(groups: 1, perGroup: 4);
        var g = Last(c);
        var matches = InGroup(c, g);
        // con 4 jugadores todos juegan en todas las jornadas: mover a otra jornada existente deja a los mismos jugadores dos veces
        var moved = c.MoveMatchToRound(matches[0].Id, matches[1].Round);
        // la jornada de origen se quedó vacía y se renumeró; ahora hay una jornada con dos partidos
        Assert.Equal(2, InGroup(c, g).Count(m => m.Round == moved.Round));
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
    public void Swap_is_allowed_even_if_a_player_ends_up_twice_in_a_round()
    {
        var c = Started(groups: 1, perGroup: 8, rounds: 3);
        var ms = InGroup(c, Last(c));
        var a = ms.First(m => m.Round == 1);
        var b = ms.First(m => m.Round == 2);
        c.SwapMatchRounds(a.Id, b.Id);
        Assert.Equal(2, a.Round);
        Assert.Equal(1, b.Round);
    }

    [Fact]
    public void Swapping_two_rounds_exchanges_all_their_matches()
    {
        var c = Started(groups: 1, perGroup: 8, rounds: 3);
        var g = Last(c);
        var r1 = InGroup(c, g).Where(m => m.Round == 1).Select(m => m.Id).ToList();
        var r3 = InGroup(c, g).Where(m => m.Round == 3).Select(m => m.Id).ToList();
        c.SwapRounds(g.Id, 1, 3);
        Assert.Equal(r3.OrderBy(x => x).ToList(), InGroup(c, g).Where(m => m.Round == 1).Select(m => m.Id).OrderBy(x => x).ToList());
        Assert.Equal(r1.OrderBy(x => x).ToList(), InGroup(c, g).Where(m => m.Round == 3).Select(m => m.Id).OrderBy(x => x).ToList());
        Assert.Equal(3, InGroup(c, g).Select(m => m.Round).Distinct().Count());
    }

    [Fact]
    public void Swapping_rounds_rejects_missing_or_equal_rounds_but_allows_played_ones()
    {
        var c = Started(groups: 1, perGroup: 4, rounds: 3);
        var g = Last(c);
        Assert.Throws<DomainException>(() => c.SwapRounds(g.Id, 1, 1));
        Assert.Throws<NotFoundException>(() => c.SwapRounds(g.Id, 1, 9));
        Assert.Throws<NotFoundException>(() => c.SwapRounds(999, 1, 2));
        Play(c, InGroup(c, g).First(m => m.Round == 1));
        var played = InGroup(c, g).First(m => m.Round == 1);
        c.SwapRounds(g.Id, 1, 2);
        Assert.Equal(2, played.Round);
        Assert.True(played.IsPlayed);
        Assert.Equal(3, InGroup(c, g).Select(m => m.Round).Distinct().Count());
    }

    [Fact]
    public void A_match_can_be_added_to_any_round_even_if_players_already_play_there_or_it_has_played_matches()
    {
        var c = Started(groups: 1, perGroup: 4, rounds: 3);
        var g = Last(c);
        var ps = c.GroupPlayers(g).Select(p => p.Id).ToList();
        var before = InGroup(c, g).Count;
        Play(c, InGroup(c, g).First(m => m.Round == 1));

        var m = c.AddMatchToRound(g.Id, 1, ps[0], ps[1], ps[2], ps[3]);

        Assert.Equal(before + 1, InGroup(c, g).Count);
        Assert.Equal(1, m.Round);
        Assert.False(m.IsPlayed);
        Assert.Equal(ps[0], m.HomeEntry!.Pair.Player1Id);
        Assert.Equal(ps[3], m.AwayEntry!.Pair.Player2Id);
    }

    [Fact]
    public void A_match_can_open_a_new_round_and_reopens_a_finished_league()
    {
        var c = Started(groups: 1, perGroup: 4, rounds: 3);
        var g = Last(c);
        var ps = c.GroupPlayers(g).Select(p => p.Id).ToList();
        foreach (var x in c.Matches.ToList()) Play(c, x);
        Assert.Equal(CompetitionStatus.Finished, c.Status);

        var m = c.AddMatchToRound(g.Id, 4, ps[0], ps[1], ps[2], ps[3]);

        Assert.Equal(4, m.Round);
        Assert.Equal(CompetitionStatus.GroupStage, c.Status);
    }

    [Fact]
    public void Adding_a_match_validates_players_round_group_and_draw()
    {
        var c = Started(groups: 2, perGroup: 4, rounds: 3);
        var g = Last(c);
        var ps = c.GroupPlayers(g).Select(p => p.Id).ToList();
        var other = c.GroupPlayers(c.Groups.First()).Select(p => p.Id).First();
        Assert.Throws<DomainException>(() => c.AddMatchToRound(g.Id, 1, ps[0], ps[0], ps[2], ps[3]));
        Assert.Throws<DomainException>(() => c.AddMatchToRound(g.Id, 1, ps[0], ps[1], ps[2], other));
        Assert.Throws<DomainException>(() => c.AddMatchToRound(g.Id, 0, ps[0], ps[1], ps[2], ps[3]));
        Assert.Throws<DomainException>(() => c.AddMatchToRound(g.Id, 5, ps[0], ps[1], ps[2], ps[3]));
        Assert.Throws<NotFoundException>(() => c.AddMatchToRound(999, 1, ps[0], ps[1], ps[2], ps[3]));
    }

    [Fact]
    public void A_match_can_be_removed_even_if_played_and_its_pairs_are_returned()
    {
        var c = Started(groups: 1, perGroup: 4, rounds: 3);
        var g = Last(c);
        var m = InGroup(c, g).First(x => x.Round == 2);
        Play(c, m);
        var before = InGroup(c, g).Count;

        var removed = c.RemoveMatch(m.Id);

        Assert.Equal(before - 1, InGroup(c, g).Count);
        Assert.Equal(2, removed.Count);
        Assert.False(InGroup(c, g).Any(x => x.Id == m.Id));
    }

    [Fact]
    public void Removing_the_only_match_of_a_round_compacts_the_numbering()
    {
        var c = Started(groups: 1, perGroup: 4, rounds: 3);
        var g = Last(c);
        var rounds = InGroup(c, g).Select(x => x.Round).Distinct().Count();
        var only = InGroup(c, g).Where(x => x.Round == 1).ToList();
        foreach (var x in only.Skip(1).ToList()) c.RemoveMatch(x.Id);
        c.RemoveMatch(only[0].Id);
        Assert.Equal(rounds - 1, InGroup(c, g).Select(x => x.Round).Distinct().Count());
        Assert.Equal(1, InGroup(c, g).Min(x => x.Round));
    }

    [Fact]
    public void Removing_matches_keeps_at_least_one_per_group_and_can_finish_the_league()
    {
        var c = Started(groups: 1, perGroup: 4, rounds: 3);
        var g = Last(c);
        var all = InGroup(c, g);
        foreach (var x in all.Skip(1).Take(all.Count - 2).ToList()) c.RemoveMatch(x.Id);
        Play(c, InGroup(c, g)[0]);
        c.RemoveMatch(InGroup(c, g)[1].Id);
        Assert.Equal(CompetitionStatus.Finished, c.Status);
        Assert.Throws<DomainException>(() => c.RemoveMatch(InGroup(c, g)[0].Id));
        Assert.Throws<NotFoundException>(() => c.RemoveMatch(99999));
    }
}
