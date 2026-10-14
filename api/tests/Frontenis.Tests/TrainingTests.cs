using Frontenis.Domain.Entities;
using Frontenis.Domain.Exceptions;
using Frontenis.Domain.Models;
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
        Assert.Throws<DomainException>(() => s.AddMatch(1, 2, 3, 3, 15, 10));   // repetido
        Assert.Throws<DomainException>(() => s.AddMatch(1, 2, 3, 4, 15, 15));   // empate
        Assert.Throws<DomainException>(() => s.AddMatch(1, 2, 3, 0, 15, 10));   // falta jugador
        s.AddMatch(1, 2, 3, 4, 15, 10);
        s.AddMatch(1, 3, 2, 4, 18, 21);
        Assert.Equal("1,2", string.Join(",", s.Matches.Select(m => m.Order)));
    }

    [Fact]
    public void Session_has_a_match_limit()
    {
        var s = Session();
        for (var i = 0; i < TrainingSession.MaxMatches; i++) s.AddMatch(1, 2, 3, 4, 15, 10);
        Assert.Throws<DomainException>(() => s.AddMatch(1, 2, 3, 4, 15, 10));
    }

    [Fact]
    public void Session_rules_and_remove()
    {
        Assert.Throws<DomainException>(() => TrainingSession.Create(" ", DateTime.Today, 3, 0));
        Assert.Throws<DomainException>(() => TrainingSession.Create("x", DateTime.Today, 2, 2));
        var s = Session();
        s.AddMatch(1, 2, 3, 4, 15, 10).WithId(7);
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

public class ReadCacheTests
{
    [Fact]
    public async Task Value_is_computed_once_until_cleared()
    {
        var cache = new Frontenis.Application.Caching.ReadCache();
        var calls = 0;
        Task<int> Load() { calls++; return Task.FromResult(calls); }
        Assert.Equal(1, await cache.GetOrAddAsync("k", Load));
        Assert.Equal(1, await cache.GetOrAddAsync("k", Load));
        cache.Clear();
        Assert.Equal(2, await cache.GetOrAddAsync("k", Load));
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task Concurrent_requests_share_one_computation()
    {
        var cache = new Frontenis.Application.Caching.ReadCache();
        var calls = 0;
        async Task<int> Slow() { Interlocked.Increment(ref calls); await Task.Delay(50); return 7; }
        var all = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => cache.GetOrAddAsync("k", Slow)));
        Assert.Equal(1, calls);
        Assert.True(all.All(v => v == 7));
    }

    [Fact]
    public async Task Errors_are_not_cached()
    {
        var cache = new Frontenis.Application.Caching.ReadCache();
        var calls = 0;
        Task<int> Fail() { calls++; throw new InvalidOperationException("x"); }
        await Assert.ThrowsAsync<InvalidOperationException>(() => cache.GetOrAddAsync("k", Fail));
        await Assert.ThrowsAsync<InvalidOperationException>(() => cache.GetOrAddAsync("k", Fail));
        Assert.Equal(2, calls);
        Assert.Equal(5, await cache.GetOrAddAsync("k", () => Task.FromResult(5)));
    }

    [Fact]
    public async Task Write_during_computation_does_not_keep_stale_result()
    {
        var cache = new Frontenis.Application.Caching.ReadCache();
        var gate = new TaskCompletionSource();
        var version = 1;
        var first = cache.GetOrAddAsync("k", async () => { var v = version; await gate.Task; return v; });
        version = 2; cache.Clear();        // alguien escribe mientras se calcula
        gate.SetResult();
        Assert.Equal(1, await first);       // esa petición recibe lo que leyó...
        Assert.Equal(2, await cache.GetOrAddAsync("k", () => Task.FromResult(version))); // ...pero no se conserva
    }

    [Fact]
    public async Task Entries_expire_after_ttl()
    {
        var clock = new ManualTime();
        var cache = new Frontenis.Application.Caching.ReadCache(clock, TimeSpan.FromHours(12), 100);
        var n = 0;
        Task<int> Load() => Task.FromResult(++n);
        Assert.Equal(1, await cache.GetOrAddAsync("k", Load));
        clock.Advance(TimeSpan.FromHours(11));
        Assert.Equal(1, await cache.GetOrAddAsync("k", Load));
        clock.Advance(TimeSpan.FromHours(2));
        Assert.Equal(2, await cache.GetOrAddAsync("k", Load));
    }

    private sealed class ManualTime : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 10, 13, 0, 0, 0, TimeSpan.Zero);
        public void Advance(TimeSpan t) => _now += t;
        public override DateTimeOffset GetUtcNow() => _now;
    }
}
