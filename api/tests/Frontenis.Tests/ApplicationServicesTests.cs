using Frontenis.Application.Dtos;
using Frontenis.Application.Interfaces.Repositories;
using Frontenis.Application.Models;
using Frontenis.Application.Services;
using Frontenis.Domain.Entities;
using Frontenis.Domain.Enums;
using Frontenis.Domain.Exceptions;
using Frontenis.Domain.Models;
using Frontenis.Domain.Services;
using Xunit;

namespace Frontenis.Tests;

// Los servicios de aplicación no escriben auditoría ni logs propios (el registro de actividad lo hace el middleware de
// la API), así que aquí la "señal" que se comprueba es el guardado (Uow.Saves) y el estado que queda en los repositorios.

internal sealed class FakeCompetitions : ICompetitionRepository
{
    private int _next = 1;
    public List<Competition> Items { get; } = new();
    public List<int> Deleted { get; } = new();

    public Task<IReadOnlyList<Competition>> ListAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Competition>>(Items.ToList());
    public Task<Competition?> GetDetailedAsync(int id, CancellationToken ct = default) =>
        Task.FromResult(Items.FirstOrDefault(c => c.Id == id));
    public Task<Competition?> GetDetailedByMatchIdAsync(int matchId, CancellationToken ct = default) =>
        Task.FromResult(Items.FirstOrDefault(c => c.Matches.Any(m => m.Id == matchId)));
    public Task AddAsync(Competition competition, CancellationToken ct = default)
    {
        competition.WithId(_next++);
        Items.Add(competition);
        return Task.CompletedTask;
    }
    public Task DeleteOrphanPairsAsync(IReadOnlyCollection<int> pairIds, CancellationToken ct = default) => Task.CompletedTask;
    public Task DeleteAsync(int id, CancellationToken ct = default)
    {
        Deleted.Add(id);
        Items.RemoveAll(c => c.Id == id);
        return Task.CompletedTask;
    }
}

internal sealed class FakeSubmissions : IMatchSubmissionRepository
{
    private int _next = 1;
    public List<MatchSubmission> Items { get; } = new();
    public List<RejectedSubmission> Rejected { get; } = new();

    public Task<MatchSubmission?> GetByMatchAsync(int matchId, CancellationToken ct = default) =>
        Task.FromResult(Items.FirstOrDefault(s => s.MatchId == matchId));
    public Task<MatchSubmission?> GetAsync(int id, CancellationToken ct = default) =>
        Task.FromResult(Items.FirstOrDefault(s => s.Id == id));
    public Task<IReadOnlyList<MatchSubmission>> ListByCompetitionAsync(int competitionId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<MatchSubmission>>(Items.ToList());
    public Task AddAsync(MatchSubmission submission, CancellationToken ct = default)
    {
        submission.WithId(_next++);
        Items.Add(submission);
        return Task.CompletedTask;
    }
    public void Remove(MatchSubmission submission) => Items.Remove(submission);
    public Task AddRejectedAsync(RejectedSubmission rejected, CancellationToken ct = default)
    {
        rejected.WithId(Rejected.Count + 1);
        Rejected.Add(rejected);
        return Task.CompletedTask;
    }
    public Task<RejectedSubmission?> GetRejectedAsync(int id, CancellationToken ct = default) =>
        Task.FromResult(Rejected.FirstOrDefault(r => r.Id == id));
    public Task<IReadOnlyList<RejectedSubmission>> ListRejectedAsync(int competitionId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<RejectedSubmission>>(Rejected.ToList());
    public void RemoveRejected(RejectedSubmission rejected) => Rejected.Remove(rejected);
}

internal sealed class FakePairs : IPairRepository
{
    private int _next = 1;
    public List<Pair> Items { get; } = new();
    public Task AddAsync(Pair pair, CancellationToken ct = default)
    {
        pair.WithId(_next++);
        Items.Add(pair);
        return Task.CompletedTask;
    }
}

internal sealed class FakeHistory : IMatchHistoryRepository
{
    public List<PlayedMatch> Matches { get; } = new();
    public Task<IReadOnlyList<PlayedMatch>> ListPlayedAsync(HistoryFilter filter, int? playerId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<PlayedMatch>>(Matches
            .Where(m => playerId is null || m.HomePlayerIds.Contains(playerId.Value) || m.AwayPlayerIds.Contains(playerId.Value))
            .ToList());
}

internal static class ServiceWorld
{
    /// <summary>Cuatro jugadores guardados (ids 1-4) y una liga de dos parejas ya sorteada con el partido 1.</summary>
    public static (FakeCompetitions Competitions, FakePlayers Players, FakeUow Uow, Competition League) LeagueWithMatch(int sets = 1)
    {
        var players = new FakePlayers();
        for (var i = 1; i <= 4; i++)
            players.Items.Add(TestHelpers.Player(i, $"J{i}", i % 2 == 0 ? PlayerPosition.Delantero : PlayerPosition.Zaguero));
        var league = Competition.Create(new CompetitionSettings("Liga", CompetitionType.League, 2, 1, 0, 3, 1, false, 0, 0, sets));
        league.AddPair(new Pair(players.Items[0], players.Items[1])).WithId(1);
        league.AddPair(new Pair(players.Items[2], players.Items[3])).WithId(2);
        league.Draw(new ShuffleGroupDistributor(new SystemRandomizer()), new RoundRobinScheduleGenerator());
        league.Matches.Single().WithId(1);
        var competitions = new FakeCompetitions();
        competitions.Items.Add(league.WithId(1));
        return (competitions, players, new FakeUow(), league);
    }
}

public class MatchSubmissionServiceTests
{
    private static SubmitResultRequest Result(int home, int away) =>
        new(new[] { new SetScoreDto(home, away) });

    private static (MatchSubmissionService Service, FakeSubmissions Submissions, FakeCompetitions Competitions, FakeUow Uow, Competition League) Build()
    {
        var (competitions, players, uow, league) = ServiceWorld.LeagueWithMatch();
        var submissions = new FakeSubmissions();
        return (new MatchSubmissionService(submissions, competitions, players, uow), submissions, competitions, uow, league);
    }

    [Fact]
    public async Task Submit_stores_a_pending_result_and_saves()
    {
        var (service, submissions, _, uow, _) = Build();

        var dto = await service.SubmitAsync(1, 1, Result(6, 4));

        Assert.Single(submissions.Items);
        Assert.Equal(1, uow.Saves);
        Assert.Equal("J1", dto.PlayerName);
        Assert.Equal(6, dto.Sets.Single().Home);
    }

    [Fact]
    public async Task Submit_twice_by_the_same_player_replaces_the_result()
    {
        var (service, submissions, _, uow, _) = Build();
        await service.SubmitAsync(1, 1, Result(6, 4));

        await service.SubmitAsync(1, 1, Result(6, 2));

        Assert.Single(submissions.Items);
        Assert.Equal(2, submissions.Items[0].Sets.Single().Away);
        Assert.Equal(2, uow.Saves);
    }

    [Fact]
    public async Task Submit_by_another_player_is_rejected_without_saving()
    {
        var (service, _, _, uow, _) = Build();
        await service.SubmitAsync(1, 1, Result(6, 4));

        await Assert.ThrowsAsync<DomainException>(() => service.SubmitAsync(1, 2, Result(6, 2)));
        Assert.Equal(1, uow.Saves);
    }

    [Fact]
    public async Task Submit_with_an_invalid_score_or_unknown_match_fails_without_saving()
    {
        var (service, _, _, uow, _) = Build();

        await Assert.ThrowsAsync<DomainException>(() => service.SubmitAsync(1, 1, Result(5, 5)));
        await Assert.ThrowsAsync<NotFoundException>(() => service.SubmitAsync(99, 1, Result(6, 4)));
        Assert.Equal(0, uow.Saves);
    }

    [Fact]
    public async Task Approve_records_the_result_removes_the_submission_and_saves()
    {
        var (service, submissions, _, uow, league) = Build();
        await service.SubmitAsync(1, 1, Result(6, 4));

        var match = await service.ApproveAsync(1);

        Assert.True(match.IsPlayed);
        Assert.Equal(6, match.HomeScore);
        Assert.Empty(submissions.Items);
        Assert.Equal(CompetitionStatus.Finished, league.Status);
        Assert.Equal(2, uow.Saves);
    }

    [Fact]
    public async Task Approve_unknown_submission_throws_NotFound()
    {
        var (service, _, _, uow, _) = Build();
        await Assert.ThrowsAsync<NotFoundException>(() => service.ApproveAsync(5));
        Assert.Equal(0, uow.Saves);
    }

    [Fact]
    public async Task Reject_moves_the_submission_to_the_rejected_list_and_leaves_the_match_unplayed()
    {
        var (service, submissions, _, uow, league) = Build();
        await service.SubmitAsync(1, 1, Result(6, 4));

        await service.RejectAsync(1);

        Assert.Empty(submissions.Items);
        var rejected = Assert.Single(submissions.Rejected);
        Assert.Equal(1, rejected.MatchId);
        Assert.False(league.Matches.Single().IsPlayed);
        Assert.Equal(2, uow.Saves);
        Assert.Single(await service.ListRejectedAsync(1));
    }

    [Fact]
    public async Task Restore_puts_a_rejected_result_back_as_pending()
    {
        var (service, submissions, _, uow, _) = Build();
        await service.SubmitAsync(1, 1, Result(6, 4));
        await service.RejectAsync(1);

        var dto = await service.RestoreAsync(1);

        Assert.Equal(1, dto.MatchId);
        Assert.Single(submissions.Items);
        Assert.Empty(submissions.Rejected);
        Assert.Equal(3, uow.Saves);
    }

    [Fact]
    public async Task Restore_fails_when_the_match_already_has_a_pending_result()
    {
        var (service, submissions, _, _, _) = Build();
        await service.SubmitAsync(1, 1, Result(6, 4));
        await service.RejectAsync(1);
        await service.SubmitAsync(1, 2, Result(6, 1));

        await Assert.ThrowsAsync<DomainException>(() => service.RestoreAsync(1));
        Assert.Single(submissions.Rejected);
    }

    [Fact]
    public async Task List_only_shows_pending_results_of_unplayed_matches_and_DeleteRejected_saves()
    {
        var (service, submissions, _, uow, _) = Build();
        await service.SubmitAsync(1, 1, Result(6, 4));
        Assert.Single(await service.ListAsync(1));
        await service.ApproveAsync(1);
        Assert.Empty(await service.ListAsync(1));

        submissions.Rejected.Add(RejectedSubmission.From(MatchSubmission.Create(1, 1, new[] { new SetScore(6, 4) }).WithId(7), 1, DateTime.UtcNow).WithId(1));
        var saves = uow.Saves;
        await service.DeleteRejectedAsync(1);
        Assert.Empty(submissions.Rejected);
        Assert.Equal(saves + 1, uow.Saves);
        await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteRejectedAsync(1));
    }
}

public class PairingServiceTests
{
    private static (PairingService Service, FakePairs Pairs, FakeCompetitions Competitions, FakePlayers Players, FakeUow Uow) Build(int playerCount = 4)
    {
        var players = new FakePlayers();
        for (var i = 1; i <= playerCount; i++)
            players.Items.Add(TestHelpers.Player(i, $"J{i}", i % 2 == 0 ? PlayerPosition.Delantero : PlayerPosition.Zaguero));
        var competitions = new FakeCompetitions();
        competitions.Items.Add(Competition.Create(new CompetitionSettings("Liga", CompetitionType.League, 4, 1, 0, 3, 1, false, 0, 0)).WithId(1));
        var pairs = new FakePairs();
        var uow = new FakeUow();
        var service = new PairingService(players, competitions, pairs, new RandomPairingStrategy(new SystemRandomizer(new Random(1))), uow);
        return (service, pairs, competitions, players, uow);
    }

    [Fact]
    public async Task Preview_pairs_players_without_saving_anything()
    {
        var (service, pairs, competitions, _, uow) = Build();

        var preview = await service.PreviewAsync(new PlayersSelection(new[] { 1, 2, 3, 4 }));

        Assert.Equal(2, preview.Pairs.Count);
        Assert.Null(preview.Unpaired);
        Assert.Empty(pairs.Items);
        Assert.Empty(competitions.Items[0].Entries);
        Assert.Equal(0, uow.Saves);
    }

    [Fact]
    public async Task Preview_with_an_odd_number_reports_the_unpaired_player()
    {
        var (service, _, _, _, _) = Build(5);
        var preview = await service.PreviewAsync(new PlayersSelection(new[] { 1, 2, 3, 4, 5 }));
        Assert.Equal(2, preview.Pairs.Count);
        Assert.NotNull(preview.Unpaired);
    }

    [Fact]
    public async Task Preview_rejects_repeated_unknown_or_too_few_players()
    {
        var (service, _, _, _, _) = Build();
        await Assert.ThrowsAsync<DomainException>(() => service.PreviewAsync(new PlayersSelection(new[] { 1, 1 })));
        await Assert.ThrowsAsync<NotFoundException>(() => service.PreviewAsync(new PlayersSelection(new[] { 1, 99 })));
        await Assert.ThrowsAsync<DomainException>(() => service.PreviewAsync(new PlayersSelection(new[] { 1 })));
    }

    [Fact]
    public async Task RegisterRandom_creates_pairs_enters_them_and_saves_once()
    {
        var (service, pairs, competitions, _, uow) = Build();

        var result = await service.RegisterRandomAsync(1, new PlayersSelection(new[] { 1, 2, 3, 4 }));

        Assert.Equal(2, result.Count);
        Assert.Equal(2, pairs.Items.Count);
        Assert.Equal(2, competitions.Items[0].Entries.Count);
        Assert.Equal(1, uow.Saves);
    }

    [Fact]
    public async Task RegisterRandom_in_a_missing_competition_fails_without_saving()
    {
        var (service, pairs, _, _, uow) = Build();
        await Assert.ThrowsAsync<NotFoundException>(() => service.RegisterRandomAsync(9, new PlayersSelection(new[] { 1, 2 })));
        Assert.Empty(pairs.Items);
        Assert.Equal(0, uow.Saves);
    }

    [Fact]
    public async Task RegisterManual_enters_the_chosen_pair_and_saves()
    {
        var (service, pairs, competitions, _, uow) = Build();

        var dto = await service.RegisterManualAsync(1, new ManualPairRequest(1, 2));

        Assert.Equal("J1 / J2", dto.Name);
        Assert.Single(pairs.Items);
        Assert.Single(competitions.Items[0].Entries);
        Assert.Equal(1, uow.Saves);
    }

    [Fact]
    public async Task RegisterManual_rejects_the_same_player_twice_and_unknown_players()
    {
        var (service, pairs, _, _, uow) = Build();
        await Assert.ThrowsAsync<DomainException>(() => service.RegisterManualAsync(1, new ManualPairRequest(1, 1)));
        await Assert.ThrowsAsync<NotFoundException>(() => service.RegisterManualAsync(1, new ManualPairRequest(1, 77)));
        Assert.Empty(pairs.Items);
        Assert.Equal(0, uow.Saves);
    }
}

public class CompetitionDrawServiceTests
{
    private static (CompetitionDrawService Service, FakeCompetitions Competitions, FakePlayers Players, FakeUow Uow) Build(int pairs)
    {
        var players = new FakePlayers();
        var competitions = new FakeCompetitions();
        var league = Competition.Create(new CompetitionSettings("Liga", CompetitionType.League, pairs, 1, 0, 3, 1, false, 0, 0)).WithId(1);
        for (var i = 1; i <= pairs; i++)
        {
            var a = TestHelpers.Player(i * 2 - 1, $"Z{i}", PlayerPosition.Zaguero);
            var b = TestHelpers.Player(i * 2, $"D{i}", PlayerPosition.Delantero);
            players.Items.Add(a);
            players.Items.Add(b);
            league.AddPair(new Pair(a, b)).WithId(i);
        }
        competitions.Items.Add(league);
        var uow = new FakeUow();
        var randomizer = new SystemRandomizer(new Random(3));
        var service = new CompetitionDrawService(competitions, players, uow, new ShuffleGroupDistributor(randomizer),
            new RoundRobinScheduleGenerator(), new StandingsCalculator(), new SingleEliminationBracketGenerator(), randomizer,
            new RotationPlanner(randomizer), new IndividualStandingsCalculator());
        return (service, competitions, players, uow);
    }

    [Fact]
    public async Task Draw_generates_the_round_robin_and_saves()
    {
        var (service, competitions, _, uow) = Build(4);

        var detail = await service.DrawAsync(1);

        Assert.Equal(6, detail.Matches.Count); // 4 parejas, todos contra todos
        Assert.Equal(CompetitionStatus.GroupStage, competitions.Items[0].Status);
        Assert.Equal(1, uow.Saves);
    }

    [Fact]
    public async Task Draw_with_the_minimum_of_two_pairs_makes_a_single_match()
    {
        var (service, _, _, _) = Build(2);
        Assert.Single((await service.DrawAsync(1)).Matches);
    }

    [Fact]
    public async Task Draw_twice_fails_the_second_time_without_saving_again()
    {
        var (service, _, _, uow) = Build(4);
        await service.DrawAsync(1);
        await Assert.ThrowsAsync<DomainException>(() => service.DrawAsync(1));
        Assert.Equal(1, uow.Saves);
    }

    [Fact]
    public async Task Draw_with_missing_pairs_or_unknown_competition_fails_without_saving()
    {
        var (service, competitions, _, uow) = Build(4);
        competitions.Items[0].RemovePairForTest();
        await Assert.ThrowsAsync<DomainException>(() => service.DrawAsync(1));
        await Assert.ThrowsAsync<NotFoundException>(() => service.DrawAsync(42));
        Assert.Equal(0, uow.Saves);
    }

    [Fact]
    public async Task DrawRotating_rejects_repeated_and_unknown_players()
    {
        var (service, _, _, uow) = Build(2);
        await Assert.ThrowsAsync<DomainException>(() => service.DrawRotatingAsync(1, new PlayersSelection(new[] { 1, 1, 2, 3 })));
        await Assert.ThrowsAsync<NotFoundException>(() => service.DrawRotatingAsync(1, new PlayersSelection(new[] { 1, 2, 3, 99 })));
        Assert.Equal(0, uow.Saves);
    }

    [Fact]
    public async Task DrawManual_with_an_unknown_entry_fails_without_saving()
    {
        var (service, _, _, uow) = Build(4);
        await Assert.ThrowsAsync<DomainException>(() =>
            service.DrawManualAsync(1, new ManualDrawRequest(new IReadOnlyList<int>[] { new[] { 1, 2, 99 } })));
        Assert.Equal(0, uow.Saves);
    }
}

internal static class CompetitionTestExtensions
{
    /// <summary>Quita una inscripción (por reflexión: el dominio no lo permite) para forzar un sorteo incompleto.</summary>
    public static void RemovePairForTest(this Competition competition)
    {
        var entries = (List<CompetitionEntry>)typeof(Competition).GetField("_entries",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(competition)!;
        entries.RemoveAt(entries.Count - 1);
    }
}

public class CompetitionCommandsTests
{
    private static CreateCompetitionRequest Request(string name = "Liga de otoño") =>
        new(name, CompetitionType.League, 4, 1, 0, 3, 1, false, 0);

    [Fact]
    public async Task Create_persists_the_competition_and_saves()
    {
        var competitions = new FakeCompetitions();
        var uow = new FakeUow();
        var commands = new CompetitionCommands(competitions, uow);

        var dto = await commands.CreateAsync(Request());

        Assert.Equal("Liga de otoño", dto.Name);
        Assert.Equal(1, dto.Id);
        Assert.Single(competitions.Items);
        Assert.Equal(1, uow.Saves);
    }

    [Fact]
    public async Task Create_with_invalid_settings_fails_without_saving()
    {
        var competitions = new FakeCompetitions();
        var uow = new FakeUow();
        var commands = new CompetitionCommands(competitions, uow);

        await Assert.ThrowsAsync<DomainException>(() => commands.CreateAsync(Request("  ")));
        Assert.Empty(competitions.Items);
        Assert.Equal(0, uow.Saves);
    }

    [Fact]
    public async Task Rename_and_ChangeSets_update_and_save()
    {
        var (competitions, _, uow, league) = ServiceWorld.LeagueWithMatch();
        var commands = new CompetitionCommands(competitions, uow);

        Assert.Equal("Nueva", (await commands.RenameAsync(1, new RenameCompetitionRequest("Nueva"))).Name);
        Assert.Equal("Nueva", league.Name);
        Assert.Equal(1, uow.Saves);

        await Assert.ThrowsAsync<DomainException>(() => commands.ChangeSetsPerMatchAsync(1, new SetsPerMatchRequest(3)));
        Assert.Equal(1, uow.Saves);
    }

    [Fact]
    public async Task Rename_of_a_missing_competition_throws_NotFound()
    {
        var commands = new CompetitionCommands(new FakeCompetitions(), new FakeUow());
        await Assert.ThrowsAsync<NotFoundException>(() => commands.RenameAsync(5, new RenameCompetitionRequest("X")));
    }

    [Fact]
    public async Task Delete_removes_an_existing_competition_and_fails_for_a_missing_one()
    {
        var (competitions, _, uow, _) = ServiceWorld.LeagueWithMatch();
        var commands = new CompetitionCommands(competitions, uow);

        await commands.DeleteAsync(1);
        Assert.Equal(new[] { 1 }, competitions.Deleted);
        Assert.Empty(competitions.Items);

        await Assert.ThrowsAsync<NotFoundException>(() => commands.DeleteAsync(1));
        Assert.Single(competitions.Deleted);
    }
}

public class PlayerHistoryServiceTests
{
    private static (PlayerHistoryService Service, FakeHistory History) Build()
    {
        var players = new FakePlayers();
        players.Items.Add(TestHelpers.Player(1, "Ana", PlayerPosition.Zaguero));
        players.Items.Add(TestHelpers.Player(2, "Beto", PlayerPosition.Delantero));
        players.Items.Add(TestHelpers.Player(3, "Carla", PlayerPosition.Zaguero));
        players.Items.Add(TestHelpers.Player(4, "Dani", PlayerPosition.Delantero));
        var history = new FakeHistory();
        return (new PlayerHistoryService(history, players, new PlayerStatsCalculator()), history);
    }

    private static PlayedMatch Match(int id, int[] home, int[] away, int h, int a, bool training = false, DateTime? date = null) =>
        new(id, 1, "Liga", true, 3, 1, home, away, new[] { new SetScore(h, a) }, training, date);

    [Fact]
    public async Task Ranking_lists_every_player_ordered_by_points_and_includes_those_without_matches()
    {
        var (service, history) = Build();
        history.Matches.Add(Match(1, new[] { 1, 2 }, new[] { 3, 4 }, 6, 4));

        var ranking = await service.RankingAsync(HistoryFilter.All);

        Assert.Equal(4, ranking.Count);
        Assert.Equal(new[] { "Ana", "Beto" }, ranking.Take(2).Select(r => r.Name).ToArray());
        Assert.All(ranking.Take(2), r => Assert.Equal(1, r.Won));
        Assert.All(ranking.Skip(2), r => Assert.Equal(1, r.Lost));
    }

    [Fact]
    public async Task Ranking_without_matches_gives_every_player_zeros_ordered_by_name()
    {
        var (service, _) = Build();
        var ranking = await service.RankingAsync(HistoryFilter.All);
        Assert.Equal(new[] { "Ana", "Beto", "Carla", "Dani" }, ranking.Select(r => r.Name).ToArray());
        Assert.All(ranking, r => Assert.Equal(0, r.Played));
    }

    [Fact]
    public async Task Player_history_shows_partners_rivals_and_recent_matches_from_the_players_side()
    {
        var (service, history) = Build();
        history.Matches.Add(Match(1, new[] { 1, 2 }, new[] { 3, 4 }, 6, 4, date: new DateTime(2026, 1, 1)));
        history.Matches.Add(Match(2, new[] { 3, 4 }, new[] { 1, 2 }, 2, 6, date: new DateTime(2026, 2, 1)));

        var dto = await service.PlayerAsync(1, HistoryFilter.All);

        Assert.Equal(2, dto.Summary.Won);
        Assert.Equal("Beto", dto.Partners.Single().Name);
        Assert.Equal(2, dto.Rivals.Count);
        Assert.Equal(new[] { 2, 1 }, dto.Recent.Select(r => r.MatchId).ToArray()); // la más reciente primero
        Assert.All(dto.Recent, r => Assert.True(r.Won));
        Assert.Equal(6, dto.Recent[0].Sets.Single().Home); // desde el punto de vista de Ana
        Assert.Equal("Beto", dto.Recent[0].Partner);
        Assert.Equal("Carla / Dani", dto.Recent[0].Rivals);
    }

    [Fact]
    public async Task Player_without_matches_has_an_empty_history_and_unknown_player_throws_NotFound()
    {
        var (service, _) = Build();
        var dto = await service.PlayerAsync(1, HistoryFilter.All);
        Assert.Equal(0, dto.Summary.Played);
        Assert.Empty(dto.Recent);
        await Assert.ThrowsAsync<NotFoundException>(() => service.PlayerAsync(99, HistoryFilter.All));
    }

    [Fact]
    public async Task A_side_with_a_single_player_is_completed_with_the_reserve_player()
    {
        var (service, history) = Build();
        history.Matches.Add(Match(1, new[] { 1 }, new[] { 3, 4 }, 6, 4));

        var recent = (await service.PlayerAsync(1, HistoryFilter.All)).Recent.Single();

        Assert.Equal(Player.ReserveName, recent.Partner);
    }

    [Fact]
    public async Task Recent_is_limited_to_fifteen_matches()
    {
        var (service, history) = Build();
        for (var i = 1; i <= 20; i++) history.Matches.Add(Match(i, new[] { 1, 2 }, new[] { 3, 4 }, 6, 4, date: new DateTime(2026, 1, 1).AddDays(i)));

        var dto = await service.PlayerAsync(1, HistoryFilter.All);

        Assert.Equal(15, dto.Recent.Count);
        Assert.Equal(20, dto.Recent[0].MatchId);
    }
}

public class SlotVisibilityTests
{
    // Jueves 15-oct-2026 a las 10:00. Esta semana: lunes 12 al domingo 18. La siguiente: 19 al 25.
    private static readonly DateTime Now = new(2026, 10, 15, 10, 0, 0);

    private static TrainingSlot Slot(DateOnly date, DateTime opensAt, bool cancelled = false)
    {
        // La apertura nunca puede ser posterior al inicio: se limita a una hora antes (sigue sin estar abierta si 'now' es anterior).
        var latest = date.ToDateTime(new TimeOnly(18, 0));
        var slot = TrainingSlot.Create(date, new TimeOnly(19, 0), new TimeOnly(20, 30), opensAt > latest ? latest : opensAt);
        if (cancelled) slot.Cancel();
        return slot;
    }

    [Theory]
    [InlineData(2026, 10, 12)] // lunes de esta semana (ya empezada o no, es de esta semana)
    [InlineData(2026, 10, 18)] // domingo de esta semana
    [InlineData(2026, 10, 19)] // lunes de la semana siguiente, aunque aún no abra
    [InlineData(2026, 10, 25)] // domingo de la semana siguiente
    public void Current_and_next_week_are_public_even_if_not_open_yet(int y, int m, int d)
    {
        var slot = Slot(new DateOnly(y, m, d), opensAt: new DateTime(2026, 12, 1, 19, 0, 0));
        Assert.True(SlotVisibility.IsPublic(slot, Now));
    }

    [Fact]
    public void A_slot_two_weeks_ahead_is_hidden_until_it_opens()
    {
        var date = new DateOnly(2026, 10, 26);
        Assert.False(SlotVisibility.IsPublic(Slot(date, opensAt: new DateTime(2026, 10, 23, 19, 0, 0)), Now));
        Assert.True(SlotVisibility.IsPublic(Slot(date, opensAt: new DateTime(2026, 10, 15, 9, 0, 0)), Now));
    }

    [Fact]
    public void A_past_week_is_hidden_and_a_cancelled_far_slot_stays_hidden()
    {
        Assert.False(SlotVisibility.IsPublic(Slot(new DateOnly(2026, 10, 8), opensAt: new DateTime(2026, 10, 1, 19, 0, 0)), Now));
        Assert.False(SlotVisibility.IsPublic(Slot(new DateOnly(2026, 10, 27), opensAt: new DateTime(2026, 10, 1, 19, 0, 0), cancelled: true), Now));
    }

    [Fact]
    public void The_week_boundary_is_decided_by_the_monday_of_now()
    {
        // Domingo 18 a las 23:59: sigue siendo la semana del 12; el lunes 26 queda fuera.
        var sunday = new DateTime(2026, 10, 18, 23, 59, 0);
        Assert.True(SlotVisibility.IsPublic(Slot(new DateOnly(2026, 10, 25), opensAt: new DateTime(2026, 12, 1)), sunday));
        Assert.False(SlotVisibility.IsPublic(Slot(new DateOnly(2026, 10, 26), opensAt: new DateTime(2026, 12, 1)), sunday));
    }
}
