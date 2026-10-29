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
public class TrainingBallTests
{
    private static TrainingSession Session() => TrainingSession.Create("Martes", new DateTime(2026, 10, 13), 3, 1);

    [Fact]
    public void Default_ball_is_pre_olympic()
    {
        Assert.Equal(TrainingBall.PreOlympic, TrainingMatch.DefaultBall);
        var match = Session().AddMatch(1, 2, 3, 4, 15, 10);
        Assert.Equal(TrainingBall.PreOlympic, match.Ball);
    }

    [Theory]
    [InlineData(TrainingBall.Slow)]
    [InlineData(TrainingBall.PreOlympic)]
    [InlineData(TrainingBall.Olympic)]
    public void The_chosen_ball_is_kept(TrainingBall ball)
    {
        var match = Session().AddMatch(1, 2, 3, 4, 15, 10, ball);
        Assert.Equal(ball, match.Ball);
    }

    [Fact]
    public void An_unknown_ball_is_rejected_and_the_match_is_not_added()
    {
        var session = Session();
        Assert.Throws<DomainException>(() => session.AddMatch(1, 2, 3, 4, 15, 10, (TrainingBall)99));
        Assert.Throws<DomainException>(() => session.AddMatch(1, 2, 3, 4, 15, 10, (TrainingBall)0));
        Assert.Empty(session.Matches);
    }

    [Fact]
    public void Values_of_the_ball_are_stable_because_they_are_stored_in_the_database()
    {
        Assert.Equal(1, (int)TrainingBall.Slow);
        Assert.Equal(2, (int)TrainingBall.PreOlympic);
        Assert.Equal(3, (int)TrainingBall.Olympic);
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
        var detail = await commands.AddMatchAsync(1, new AddTrainingMatchRequest(1, 3, 2, 4, 15, 12, TrainingBall.Slow));

        Assert.Equal(new[] { TrainingBall.PreOlympic, TrainingBall.Slow }, detail.Matches.Select(m => m.Ball).ToArray());
        Assert.Equal(2, uow.Saves);
    }

    [Fact]
    public async Task An_invalid_ball_fails_without_saving()
    {
        var (commands, uow) = Build();
        await Assert.ThrowsAsync<DomainException>(() =>
            commands.AddMatchAsync(1, new AddTrainingMatchRequest(1, 2, 3, 4, 15, 10, (TrainingBall)7)));
        Assert.Equal(0, uow.Saves);
    }

    [Fact]
    public void The_history_filter_uses_the_pre_olympic_ball_unless_another_one_is_chosen()
    {
        Assert.Equal(TrainingBall.PreOlympic, HistoryFilter.All.Ball);
        Assert.Equal(TrainingBall.PreOlympic, new HistoryFilter(new[] { 1 }, Array.Empty<int>()).Ball);
        Assert.Equal(TrainingBall.Slow, new HistoryFilter(Array.Empty<int>(), Array.Empty<int>(), TrainingBall.Slow).Ball);
    }

    [Fact]
    public void The_cached_history_is_kept_apart_for_each_ball()
    {
        var keys = new[] { TrainingBall.Slow, TrainingBall.PreOlympic, TrainingBall.Olympic }
            .Select(b => CacheKeys.HistoryRanking(new HistoryFilter(Array.Empty<int>(), Array.Empty<int>(), b))).ToList();
        Assert.Equal(3, keys.Distinct().Count());
        Assert.Equal(3, new[] { TrainingBall.Slow, TrainingBall.PreOlympic, TrainingBall.Olympic }
            .Select(b => CacheKeys.HistoryPlayer(7, new HistoryFilter(Array.Empty<int>(), Array.Empty<int>(), b))).Distinct().Count());
    }
}
