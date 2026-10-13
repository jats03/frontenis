using Frontenis.Domain.Entities;
using Frontenis.Domain.Exceptions;
using Frontenis.Domain.Services;
using Xunit;

namespace Frontenis.Tests;

public class TrainingTests
{
    private static TrainingSession Session() => TrainingSession.Create("Martes", new DateTime(2026, 10, 13), 3, 1);
    private static TrainingGame G(int h1, int h2, int a1, int a2, int hs, int a) => new(h1, h2, a1, a2, hs, a, 15);

    [Fact]
    public void Match_validations()
    {
        var s = Session();
        Assert.Throws<DomainException>(() => s.AddMatch(1, 2, 3, 3, 15, 15, 10));   // repetido
        Assert.Throws<DomainException>(() => s.AddMatch(1, 2, 3, 4, 15, 15, 15));   // empate
        Assert.Throws<DomainException>(() => s.AddMatch(1, 2, 3, 0, 15, 15, 10));   // falta jugador
        Assert.Throws<DomainException>(() => s.AddMatch(1, 2, 3, 4, 0, 15, 10));    // objetivo
        s.AddMatch(1, 2, 3, 4, 15, 15, 10);
        s.AddMatch(1, 3, 2, 4, 21, 18, 21);
        Assert.Equal("1,2", string.Join(",", s.Matches.Select(m => m.Order)));
    }

    [Fact]
    public void Session_rules_and_remove()
    {
        Assert.Throws<DomainException>(() => TrainingSession.Create(" ", DateTime.Today, 3, 0));
        Assert.Throws<DomainException>(() => TrainingSession.Create("x", DateTime.Today, 2, 2));
        var s = Session();
        s.AddMatch(1, 2, 3, 4, 15, 15, 10).WithId(7);
        Assert.Throws<NotFoundException>(() => s.RemoveMatch(99));
        s.RemoveMatch(7);
        Assert.Empty(s.Matches);
    }

    [Fact]
    public void Pair_and_individual_standings()
    {
        var games = new List<TrainingGame> { G(1, 2, 3, 4, 15, 10), G(2, 1, 5, 6, 15, 12), G(1, 3, 2, 4, 9, 15) };
        var pairs = TrainingStandings.ByPair(games, 3, 1);
        var top = pairs.First();
        Assert.Equal((1, 2), (top.Player1Id, top.Player2Id));   // la pareja 1-2 es la misma aunque cambie el orden
        Assert.Equal(2, top.Played);
        Assert.Equal(6, top.ClassificationPoints);

        var players = TrainingStandings.ByPlayer(games, 3, 1);
        var p1 = players.Single(r => r.Player1Id == 1);
        Assert.Equal(3, p1.Played);
        Assert.Equal(2, p1.Won);
        Assert.Equal(7, p1.ClassificationPoints);   // 3 + 3 + 1
        Assert.Equal(1, players.First().Rank);
    }

    [Fact]
    public void Suggestions_need_four_players_and_prefer_balanced_split()
    {
        var planner = new TrainingPlanner();
        Assert.Empty(planner.Suggest(new[] { 1, 2, 3 }, new List<TrainingGame>(), 3));

        // 1 y 2 son fuertes; 3 y 4 flojos → lo equilibrado es mezclarlos (1 con 3/4, 2 con el otro).
        var games = new List<TrainingGame>();
        for (var i = 0; i < 4; i++) { games.Add(G(1, 5, 3, 6, 15, 5)); games.Add(G(2, 6, 4, 5, 15, 6)); }
        var best = planner.Suggest(new[] { 1, 2, 3, 4 }, games, 3)[0];
        var sideOf1 = new[] { best.Home1, best.Home2 }.Contains(1) ? new[] { best.Home1, best.Home2 } : new[] { best.Away1, best.Away2 };
        Assert.True(!sideOf1.Contains(2), "los dos fuertes no deben ir juntos");
        Assert.True(best.Balance >= 80);
    }

    [Fact]
    public void Suggestions_favor_players_who_have_played_less()
    {
        var planner = new TrainingPlanner();
        var games = new List<TrainingGame> { G(1, 2, 3, 4, 15, 10), G(1, 3, 2, 4, 15, 10) };
        var best = planner.Suggest(new[] { 1, 2, 3, 4, 5, 6 }, games, 1)[0];
        var chosen = new[] { best.Home1, best.Home2, best.Away1, best.Away2 };
        Assert.Contains(5, chosen);
        Assert.Contains(6, chosen);
    }

    [Fact]
    public void Suggestions_are_deterministic_and_limited()
    {
        var planner = new TrainingPlanner();
        var a = planner.Suggest(new[] { 1, 2, 3, 4, 5 }, new List<TrainingGame>(), 4);
        var b = planner.Suggest(new[] { 5, 4, 3, 2, 1 }, new List<TrainingGame>(), 4);
        Assert.Equal(4, a.Count);
        Assert.True(a.Select(x => (x.Home1, x.Home2, x.Away1, x.Away2)).SequenceEqual(b.Select(x => (x.Home1, x.Home2, x.Away1, x.Away2))));
    }
}
