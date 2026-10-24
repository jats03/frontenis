using Frontenis.Application.Models;
using Frontenis.Domain.Entities;
using Frontenis.Domain.Enums;
using Frontenis.Domain.Exceptions;
using Frontenis.Domain.Models;
using Frontenis.Domain.Services;
using Frontenis.Infrastructure.Repositories;
using Xunit;

namespace Frontenis.Tests;

/// <summary>Repositorios de EF Core contra SQLite en memoria (ver <see cref="SqliteDb"/> para las limitaciones).</summary>
public class PlayerRepositoryTests
{
    [Fact]
    public async Task List_orders_ignoring_case_and_accents_and_hides_the_reserve()
    {
        using var sqlite = new SqliteDb();
        await using (var db = sqlite.NewContext())
        {
            var repo = new PlayerRepository(db);
            foreach (var n in new[] { "zoe", "Álvaro", "alberto", "Éric" })
                await repo.AddAsync(new Player(n, PlayerPosition.Zaguero));
            await repo.GetOrCreateReserveAsync();
            await db.SaveChangesAsync();
        }
        await using var read = sqlite.NewContext();
        var names = (await new PlayerRepository(read).ListAsync()).Select(p => p.Name).ToArray();
        Assert.Equal(new[] { "alberto", "Álvaro", "Éric", "zoe" }, names);
    }

    [Fact]
    public async Task GetOrCreateReserve_creates_it_once()
    {
        using var sqlite = new SqliteDb();
        await using (var db = sqlite.NewContext())
        {
            await new PlayerRepository(db).GetOrCreateReserveAsync();
            await db.SaveChangesAsync();
        }
        await using var other = sqlite.NewContext();
        var again = await new PlayerRepository(other).GetOrCreateReserveAsync();
        Assert.True(again.IsReserve);
        Assert.Equal(1, other.Players.Count(p => p.IsReserve));
    }

    [Fact]
    public async Task GetByIds_keeps_the_requested_order_and_skips_unknown_ids()
    {
        using var sqlite = new SqliteDb();
        await using var db = sqlite.NewContext();
        var repo = new PlayerRepository(db);
        var a = new Player("A", PlayerPosition.Zaguero);
        var b = new Player("B", PlayerPosition.Delantero);
        await repo.AddAsync(a);
        await repo.AddAsync(b);
        await db.SaveChangesAsync();

        var found = await repo.GetByIdsAsync(new[] { b.Id, 999, a.Id });

        Assert.Equal(new[] { "B", "A" }, found.Select(p => p.Name).ToArray());
    }

    [Fact]
    public async Task IsInUse_is_true_only_for_players_with_pairs_or_training_matches()
    {
        using var sqlite = new SqliteDb();
        await using var db = sqlite.NewContext();
        var players = Enumerable.Range(1, 6).Select(i => new Player($"P{i}", PlayerPosition.Zaguero)).ToList();
        db.Players.AddRange(players);
        await db.SaveChangesAsync();
        db.Pairs.Add(new Pair(players[0], players[1]));
        var session = TrainingSession.Create("Entreno", new DateTime(2025, 1, 7), 1, 0);
        session.AddMatch(players[2].Id, players[3].Id, players[4].Id, players[0].Id + 100, 6, 4);
        db.TrainingSessions.Add(session);
        await db.SaveChangesAsync();
        var repo = new PlayerRepository(db);

        Assert.True(await repo.IsInUseAsync(players[0].Id));
        Assert.True(await repo.IsInUseAsync(players[2].Id));
        Assert.True(await repo.IsInUseAsync(players[4].Id));
        Assert.False(await repo.IsInUseAsync(players[5].Id));
    }
}

public class CompetitionRepositoryTests
{
    private static Competition NewLeague(Player a, Player b, Player c, Player d)
    {
        var league = Competition.Create(new CompetitionSettings("Liga", CompetitionType.League, 2, 1, 0, 3, 1, false, 0, 0));
        league.AddPair(new Pair(a, b));
        league.AddPair(new Pair(c, d));
        league.Draw(new ShuffleGroupDistributor(new SystemRandomizer()), new RoundRobinScheduleGenerator());
        return league;
    }

    /// <summary>Guarda una liga de dos parejas ya sorteada y devuelve su id.</summary>
    private static async Task<int> SeedLeagueAsync(SqliteDb sqlite)
    {
        await using var db = sqlite.NewContext();
        var p = Enumerable.Range(1, 4).Select(i => new Player($"J{i}", PlayerPosition.Zaguero)).ToList();
        db.Players.AddRange(p);
        await db.SaveChangesAsync();
        var c = NewLeague(p[0], p[1], p[2], p[3]);
        await new CompetitionRepository(db).AddAsync(c);
        await db.SaveChangesAsync();
        return c.Id;
    }

    [Fact]
    public async Task Detailed_loads_groups_entries_pairs_players_and_matches()
    {
        using var sqlite = new SqliteDb();
        var id = await SeedLeagueAsync(sqlite);
        await using var db = sqlite.NewContext();

        var c = await new CompetitionRepository(db).GetDetailedAsync(id);

        Assert.NotNull(c);
        Assert.Equal(2, c!.Entries.Count);
        Assert.All(c.Entries, e => Assert.False(string.IsNullOrEmpty(e.Pair.Player1.Name)));
        Assert.Single(c.Matches);
        Assert.Single(c.Groups);
    }

    [Fact]
    public async Task Detailed_returns_null_when_it_does_not_exist()
    {
        using var sqlite = new SqliteDb();
        await using var db = sqlite.NewContext();
        Assert.Null(await new CompetitionRepository(db).GetDetailedAsync(404));
    }

    [Fact]
    public async Task Detailed_by_match_id_finds_the_owning_competition()
    {
        using var sqlite = new SqliteDb();
        var id = await SeedLeagueAsync(sqlite);
        await using var db = sqlite.NewContext();
        var matchId = db.Matches.Single().Id;
        var repo = new CompetitionRepository(db);

        Assert.Equal(id, (await repo.GetDetailedByMatchIdAsync(matchId))!.Id);
        Assert.Null(await repo.GetDetailedByMatchIdAsync(9999));
    }

    [Fact]
    public async Task A_recorded_result_survives_a_round_trip()
    {
        using var sqlite = new SqliteDb();
        var id = await SeedLeagueAsync(sqlite);
        await using (var db = sqlite.NewContext())
        {
            var c = (await new CompetitionRepository(db).GetDetailedAsync(id))!;
            c.RecordResult(c.Matches.Single().Id, 6, 4);
            await db.SaveChangesAsync();
        }
        await using var read = sqlite.NewContext();
        var saved = (await new CompetitionRepository(read).GetDetailedAsync(id))!;
        var m = saved.Matches.Single();
        Assert.True(m.HomeScore == 6 && m.AwayScore == 4, sqlite.SqlLog());
        Assert.Equal(CompetitionStatus.Finished, saved.Status);
    }

    [Fact]
    public async Task List_contains_every_competition_newest_first()
    {
        using var sqlite = new SqliteDb();
        var first = await SeedLeagueAsync(sqlite);
        await using (var db = sqlite.NewContext())
        {
            var p = Enumerable.Range(1, 4).Select(i => new Player($"K{i}", PlayerPosition.Delantero)).ToList();
            db.Players.AddRange(p);
            await db.SaveChangesAsync();
            await new CompetitionRepository(db).AddAsync(NewLeague(p[0], p[1], p[2], p[3]));
            await db.SaveChangesAsync();
        }
        await using var read = sqlite.NewContext();
        var list = await new CompetitionRepository(read).ListAsync();
        Assert.Equal(2, list.Count);
        Assert.True(list[0].CreatedAtUtc >= list[1].CreatedAtUtc);
        Assert.Contains(list, c => c.Id == first);
    }

    [Fact]
    public async Task Delete_removes_the_competition_with_its_matches_and_entries_but_keeps_players()
    {
        using var sqlite = new SqliteDb();
        var id = await SeedLeagueAsync(sqlite);
        await using (var db = sqlite.NewContext())
            await new CompetitionRepository(db).DeleteAsync(id);

        await using var read = sqlite.NewContext();
        Assert.Empty(read.Competitions);
        Assert.Empty(read.Matches);
        Assert.Empty(read.Entries);
        Assert.Empty(read.Groups);
        Assert.Equal(4, read.Players.Count());
    }

    [Fact]
    public async Task Delete_of_a_missing_competition_throws_NotFound_and_changes_nothing()
    {
        using var sqlite = new SqliteDb();
        var id = await SeedLeagueAsync(sqlite);
        await using var db = sqlite.NewContext();
        await Assert.ThrowsAsync<NotFoundException>(() => new CompetitionRepository(db).DeleteAsync(id + 100));
        await using var read = sqlite.NewContext();
        Assert.Single(read.Competitions);
        Assert.Single(read.Matches);
    }
}

public class MatchHistoryRepositoryTests
{
    private static async Task<(List<Player> Players, int CompetitionId)> SeedAsync(SqliteDb sqlite)
    {
        await using var db = sqlite.NewContext();
        var players = Enumerable.Range(1, 4).Select(i => new Player($"H{i}", PlayerPosition.Zaguero)).ToList();
        db.Players.AddRange(players);
        await db.SaveChangesAsync();
        var c = Competition.Create(new CompetitionSettings("Liga", CompetitionType.League, 2, 1, 0, 3, 1, false, 0, 0));
        c.AddPair(new Pair(players[0], players[1]));
        c.AddPair(new Pair(players[2], players[3]));
        c.Draw(new ShuffleGroupDistributor(new SystemRandomizer()), new RoundRobinScheduleGenerator());
        db.Competitions.Add(c);
        var session = TrainingSession.Create("Entreno", new DateTime(2025, 1, 7), 1, 0);
        session.AddMatch(players[0].Id, players[2].Id, players[1].Id, players[3].Id, 6, 3);
        db.TrainingSessions.Add(session);
        await db.SaveChangesAsync();
        c.RecordResult(c.Matches.Single().Id, 6, 4);
        await db.SaveChangesAsync();
        return (players, c.Id);
    }

    [Fact]
    public async Task All_returns_played_competition_and_training_matches()
    {
        using var sqlite = new SqliteDb();
        await SeedAsync(sqlite);
        await using var db = sqlite.NewContext();

        var played = await new MatchHistoryRepository(db).ListPlayedAsync(HistoryFilter.All, null);

        Assert.True(played.Count == 2, sqlite.SqlLog());
        Assert.Single(played, m => m.IsTraining);
        Assert.Single(played, m => !m.IsTraining);
    }

    [Fact]
    public async Task Filtering_by_competition_excludes_trainings()
    {
        using var sqlite = new SqliteDb();
        var (_, competitionId) = await SeedAsync(sqlite);
        await using var db = sqlite.NewContext();

        var played = await new MatchHistoryRepository(db)
            .ListPlayedAsync(new HistoryFilter(new[] { competitionId }, Array.Empty<int>()), null);

        Assert.False(Assert.Single(played).IsTraining);
    }

    [Fact]
    public async Task Filtering_by_player_keeps_only_that_players_matches()
    {
        using var sqlite = new SqliteDb();
        var (players, _) = await SeedAsync(sqlite);
        await using var db = sqlite.NewContext();
        var repo = new MatchHistoryRepository(db);

        // Los cuatro juegan ambos partidos; un jugador inexistente no juega ninguno.
        Assert.Equal(2, (await repo.ListPlayedAsync(HistoryFilter.All, players[0].Id)).Count);
        Assert.Empty(await repo.ListPlayedAsync(HistoryFilter.All, 9999));
    }

    [Fact]
    public async Task Unplayed_competition_matches_are_not_listed()
    {
        using var sqlite = new SqliteDb();
        await using (var db = sqlite.NewContext())
        {
            var p = Enumerable.Range(1, 4).Select(i => new Player($"U{i}", PlayerPosition.Delantero)).ToList();
            db.Players.AddRange(p);
            await db.SaveChangesAsync();
            var c = Competition.Create(new CompetitionSettings("Liga", CompetitionType.League, 2, 1, 0, 3, 1, false, 0, 0));
            c.AddPair(new Pair(p[0], p[1]));
            c.AddPair(new Pair(p[2], p[3]));
            c.Draw(new ShuffleGroupDistributor(new SystemRandomizer()), new RoundRobinScheduleGenerator());
            db.Competitions.Add(c);
            await db.SaveChangesAsync();
        }
        await using var read = sqlite.NewContext();
        Assert.Empty(await new MatchHistoryRepository(read).ListPlayedAsync(HistoryFilter.All, null));
    }
}
