using Frontenis.Application.Dtos;
using Frontenis.Application.Services;
using Frontenis.Domain.Entities;
using Frontenis.Domain.Enums;
using Frontenis.Domain.Exceptions;
using Xunit;

namespace Frontenis.Tests;

/// <summary>El administrador corrige jugadores, marcador y bola de un partido de entrenamiento ya guardado.</summary>
public class TrainingMatchEditTests
{
    private static TrainingSession SessionWithMatch(out TrainingMatch match)
    {
        var s = TrainingSession.Create("Martes", new DateTime(2026, 10, 13), 3, 1);
        match = s.AddMatch(1, 2, 3, 4, 15, 9, BallType.Slow).WithId(1);
        return s;
    }

    [Fact]
    public void Players_score_and_ball_are_replaced_but_order_and_time_are_kept()
    {
        var session = SessionWithMatch(out var match);
        var playedAt = match.PlayedAtUtc;
        var order = match.Order;

        session.UpdateMatch(1, 5, 6, 7, 8, 11, 15, BallType.Olympic);

        Assert.Equal(new[] { 5, 6, 7, 8 }, new[] { match.Home1Id, match.Home2Id, match.Away1Id, match.Away2Id });
        Assert.Equal((11, 15), (match.HomeScore, match.AwayScore));
        Assert.False(match.HomeWon);
        Assert.Equal(BallType.Olympic, match.Ball);
        Assert.Equal(playedAt, match.PlayedAtUtc);
        Assert.Equal(order, match.Order);
    }

    [Fact]
    public void Only_one_thing_can_change_at_a_time()
    {
        var session = SessionWithMatch(out var match);

        session.UpdateMatch(1, 1, 3, 2, 4, 15, 9, BallType.Slow); // solo las parejas
        Assert.Equal(new[] { 1, 3, 2, 4 }, new[] { match.Home1Id, match.Home2Id, match.Away1Id, match.Away2Id });
        Assert.Equal((15, 9, BallType.Slow), (match.HomeScore, match.AwayScore, match.Ball));

        session.UpdateMatch(1, 1, 3, 2, 4, 15, 10, BallType.Slow); // solo el marcador
        Assert.Equal((15, 10), (match.HomeScore, match.AwayScore));

        session.UpdateMatch(1, 1, 3, 2, 4, 15, 10, BallType.PreOlympic); // solo la bola
        Assert.Equal(BallType.PreOlympic, match.Ball);
    }

    [Theory]
    [InlineData(1, 1, 3, 4, 15, 9, BallType.Slow)]   // jugador repetido en la misma pareja
    [InlineData(1, 2, 2, 4, 15, 9, BallType.Slow)]   // repetido entre parejas
    [InlineData(0, 2, 3, 4, 15, 9, BallType.Slow)]   // falta un jugador
    [InlineData(1, 2, 3, 4, 10, 10, BallType.Slow)]  // empate
    [InlineData(1, 2, 3, 4, -1, 9, BallType.Slow)]   // marcador negativo
    [InlineData(1, 2, 3, 4, 1000, 9, BallType.Slow)] // marcador excesivo
    [InlineData(1, 2, 3, 4, 15, 9, (BallType)42)]    // bola desconocida
    public void Invalid_data_is_rejected_and_nothing_changes(int h1, int h2, int a1, int a2, int hs, int aws, BallType ball)
    {
        var session = SessionWithMatch(out var match);

        Assert.Throws<DomainException>(() => session.UpdateMatch(1, h1, h2, a1, a2, hs, aws, ball));

        Assert.Equal(new[] { 1, 2, 3, 4 }, new[] { match.Home1Id, match.Home2Id, match.Away1Id, match.Away2Id });
        Assert.Equal((15, 9, BallType.Slow), (match.HomeScore, match.AwayScore, match.Ball));
    }

    [Fact]
    public void An_unknown_match_throws_NotFound()
    {
        var session = SessionWithMatch(out _);
        Assert.Throws<NotFoundException>(() => session.UpdateMatch(99, 1, 2, 3, 4, 15, 9, BallType.Slow));
    }

    private static (TrainingCommands Commands, FakeUow Uow) Build(int players = 8)
    {
        var fakePlayers = new FakePlayers();
        for (var i = 1; i <= players; i++) fakePlayers.Items.Add(TestHelpers.Player(i, $"J{i}", PlayerPosition.Zaguero));
        var trainings = new FakeTrainings();
        trainings.Items.Add(SessionWithMatch(out _).WithId(1));
        var uow = new FakeUow();
        return (new TrainingCommands(trainings, fakePlayers, new TrainingDetailBuilder(fakePlayers), uow), uow);
    }

    [Fact]
    public async Task The_service_saves_and_the_detail_and_standings_use_the_corrected_match()
    {
        var (commands, uow) = Build();

        var detail = await commands.UpdateMatchAsync(1, 1, new UpdateTrainingMatchRequest(5, 6, 7, 8, 9, 15, BallType.Olympic));

        var m = detail.Matches.Single();
        Assert.Equal(("J5 / J6", "J7 / J8"), (m.HomeName, m.AwayName));
        Assert.Equal((9, 15, false, BallType.Olympic), (m.HomeScore, m.AwayScore, m.HomeWon, m.Ball));
        Assert.Contains(detail.PlayerStandings, r => r.PlayerIds.Contains(7) && r.Won == 1);
        Assert.Contains(detail.PlayerStandings, r => r.PlayerIds.Contains(5) && r.Lost == 1);
        Assert.DoesNotContain(detail.PlayerStandings, r => r.PlayerIds.Contains(1));
        Assert.Equal(1, uow.Saves);
    }

    [Fact]
    public async Task An_unknown_player_or_match_or_invalid_data_fails_without_saving()
    {
        var (commands, uow) = Build(players: 6);

        await Assert.ThrowsAsync<NotFoundException>(() => commands.UpdateMatchAsync(1, 1, new(5, 6, 7, 8, 15, 9, BallType.Slow)));  // 7 y 8 no existen
        await Assert.ThrowsAsync<NotFoundException>(() => commands.UpdateMatchAsync(1, 99, new(1, 2, 3, 4, 15, 9, BallType.Slow))); // partido inexistente
        await Assert.ThrowsAsync<NotFoundException>(() => commands.UpdateMatchAsync(42, 1, new(1, 2, 3, 4, 15, 9, BallType.Slow))); // entreno inexistente
        await Assert.ThrowsAsync<DomainException>(() => commands.UpdateMatchAsync(1, 1, new(1, 1, 3, 4, 15, 9, BallType.Slow)));    // repetidos
        await Assert.ThrowsAsync<DomainException>(() => commands.UpdateMatchAsync(1, 1, new(1, 2, 3, 4, 9, 9, BallType.Slow)));     // empate
        await Assert.ThrowsAsync<DomainException>(() => commands.UpdateMatchAsync(1, 1, new(1, 2, 3, 4, 15, 9, (BallType)0)));      // bola no válida
        Assert.Equal(0, uow.Saves);
    }
}
