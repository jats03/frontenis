using Frontenis.Application.Caching;
using Frontenis.Application.Dtos;
using Frontenis.Application.Models;
using Frontenis.Application.Services;
using Frontenis.Domain.Entities;
using Frontenis.Domain.Enums;
using Frontenis.Domain.Exceptions;
using Xunit;

namespace Frontenis.Tests;

/// <summary>Tipo de bola de los partidos de entrenamiento: por defecto preolímpica, y la que se elija se guarda.</summary>
public class BallTypeTests
{
    private static TrainingSession Session() => TrainingSession.Create("Martes", new DateTime(2026, 10, 13), 3, 1);

    [Fact]
    public void Default_ball_is_pre_olympic()
    {
        Assert.Equal(BallType.PreOlympic, BallTypes.Default);
        var match = Session().AddMatch(1, 2, 3, 4, 15, 10);
        Assert.Equal(BallType.PreOlympic, match.Ball);
    }

    [Theory]
    [InlineData(BallType.Slow)]
    [InlineData(BallType.PreOlympic)]
    [InlineData(BallType.Olympic)]
    public void The_chosen_ball_is_kept(BallType ball)
    {
        var match = Session().AddMatch(1, 2, 3, 4, 15, 10, ball);
        Assert.Equal(ball, match.Ball);
    }

    [Fact]
    public void An_unknown_ball_is_rejected_and_the_match_is_not_added()
    {
        var session = Session();
        Assert.Throws<DomainException>(() => session.AddMatch(1, 2, 3, 4, 15, 10, (BallType)99));
        Assert.Throws<DomainException>(() => session.AddMatch(1, 2, 3, 4, 15, 10, (BallType)0));
        Assert.Empty(session.Matches);
    }

    [Fact]
    public void Values_of_the_ball_are_stable_because_they_are_stored_in_the_database()
    {
        Assert.Equal(1, (int)BallType.Slow);
        Assert.Equal(2, (int)BallType.PreOlympic);
        Assert.Equal(3, (int)BallType.Olympic);
    }

    private static (TrainingCommands Commands, FakeUow Uow) Build()
    {
        var players = new FakePlayers();
        for (var i = 1; i <= 4; i++) players.Items.Add(TestHelpers.Player(i, $"J{i}", PlayerPosition.Zaguero));
        var trainings = new FakeTrainings();
        trainings.Items.Add(Session().WithId(1));
        var uow = new FakeUow();
        return (new TrainingCommands(trainings, players, new TrainingDetailBuilder(players), uow), uow);
    }

    [Fact]
    public async Task The_detail_returns_the_ball_of_every_match_and_saves()
    {
        var (commands, uow) = Build();

        await commands.AddMatchAsync(1, new AddTrainingMatchRequest(1, 2, 3, 4, 15, 10));
        var detail = await commands.AddMatchAsync(1, new AddTrainingMatchRequest(1, 3, 2, 4, 15, 12, BallType.Slow));

        Assert.Equal(new[] { BallType.PreOlympic, BallType.Slow }, detail.Matches.Select(m => m.Ball).ToArray());
        Assert.Equal(2, uow.Saves);
    }

    [Fact]
    public async Task An_invalid_ball_fails_without_saving()
    {
        var (commands, uow) = Build();
        await Assert.ThrowsAsync<DomainException>(() =>
            commands.AddMatchAsync(1, new AddTrainingMatchRequest(1, 2, 3, 4, 15, 10, (BallType)7)));
        Assert.Equal(0, uow.Saves);
    }

    [Fact]
    public void The_history_filter_counts_every_ball_unless_some_are_chosen()
    {
        Assert.True(HistoryFilter.All.AllBalls);
        Assert.True(new HistoryFilter(new[] { 1 }, Array.Empty<int>(), Array.Empty<BallType>()).AllBalls);
        var some = new HistoryFilter(Array.Empty<int>(), Array.Empty<int>(), new[] { BallType.Slow, BallType.Olympic });
        Assert.False(some.AllBalls);
        Assert.Equal(2, some.Balls!.Count);
    }

    [Fact]
    public void The_cached_history_is_kept_apart_for_each_combination_of_balls()
    {
        static HistoryFilter F(params BallType[] balls) => new(Array.Empty<int>(), Array.Empty<int>(), balls);
        var combos = new[] { F(), F(BallType.Slow), F(BallType.PreOlympic), F(BallType.Olympic), F(BallType.Slow, BallType.Olympic) };
        Assert.Equal(combos.Length, combos.Select(CacheKeys.HistoryRanking).Distinct().Count());
        Assert.Equal(combos.Length, combos.Select(f => CacheKeys.HistoryPlayer(7, f)).Distinct().Count());
        // el orden en que se marcan las bolas no cambia la clave
        Assert.Equal(CacheKeys.HistoryRanking(F(BallType.Slow, BallType.Olympic)), CacheKeys.HistoryRanking(F(BallType.Olympic, BallType.Slow)));
    }

    // ---------- Competiciones: la bola se elige al registrar el resultado ----------

    private static Competition League()
    {
        var (_, _, _, league) = ServiceWorld.LeagueWithMatch();
        return league;
    }

    [Fact]
    public void A_competition_match_is_pre_olympic_until_another_ball_is_chosen()
    {
        var league = League();
        Assert.Equal(BallType.PreOlympic, league.Matches.Single().Ball);

        Assert.Equal(BallType.PreOlympic, league.RecordResult(1, 6, 4).Ball);
    }

    [Theory]
    [InlineData(BallType.Slow)]
    [InlineData(BallType.PreOlympic)]
    [InlineData(BallType.Olympic)]
    public void The_ball_chosen_with_the_result_is_kept(BallType ball)
    {
        Assert.Equal(ball, League().RecordResult(1, 6, 4, ball).Ball);
    }

    [Fact]
    public void Correcting_a_result_without_a_ball_keeps_the_ball_and_clearing_it_restores_the_default()
    {
        var league = League();
        league.RecordResult(1, 6, 4, BallType.Slow);

        Assert.Equal(BallType.Slow, league.RecordResult(1, 6, 3).Ball);
        Assert.Equal(BallType.PreOlympic, league.ClearResult(1).Ball);
    }

    [Fact]
    public void An_unknown_ball_in_a_competition_is_rejected_and_the_result_is_not_saved()
    {
        var league = League();
        Assert.Throws<DomainException>(() => league.RecordResult(1, 6, 4, (BallType)42));
        Assert.False(league.Matches.Single().IsPlayed);
    }

    [Fact]
    public async Task The_ball_travels_with_a_result_submitted_by_a_player_until_it_is_approved()
    {
        var (competitions, players, uow, league) = ServiceWorld.LeagueWithMatch();
        var submissions = new FakeSubmissions();
        var service = new MatchSubmissionService(submissions, competitions, players, uow);

        var sent = await service.SubmitAsync(1, 1, new SubmitResultRequest(new[] { new SetScoreDto(6, 4) }, BallType.Olympic));
        Assert.Equal(BallType.Olympic, sent.Ball);
        Assert.Equal(BallType.Olympic, (await service.ListAsync(1)).Single().Ball);

        var match = await service.ApproveAsync(sent.Id);
        Assert.Equal(BallType.Olympic, match.Ball);
        Assert.Equal(BallType.Olympic, league.Matches.Single().Ball);
    }

    [Fact]
    public async Task A_rejected_result_keeps_its_ball_when_it_is_restored()
    {
        var (competitions, players, uow, _) = ServiceWorld.LeagueWithMatch();
        var submissions = new FakeSubmissions();
        var service = new MatchSubmissionService(submissions, competitions, players, uow);
        var sent = await service.SubmitAsync(1, 1, new SubmitResultRequest(new[] { new SetScoreDto(6, 4) }, BallType.Slow));
        await service.RejectAsync(sent.Id);

        Assert.Equal(BallType.Slow, (await service.ListRejectedAsync(1)).Single().Ball);
        Assert.Equal(BallType.Slow, (await service.RestoreAsync(1)).Ball);
    }

    [Fact]
    public async Task A_submission_without_a_ball_is_pre_olympic()
    {
        var (competitions, players, uow, _) = ServiceWorld.LeagueWithMatch();
        var service = new MatchSubmissionService(new FakeSubmissions(), competitions, players, uow);
        var sent = await service.SubmitAsync(1, 1, new SubmitResultRequest(new[] { new SetScoreDto(6, 4) }));
        Assert.Equal(BallType.PreOlympic, sent.Ball);
    }
}
