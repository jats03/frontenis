using Frontenis.Application.Dtos;
using Frontenis.Application.Services;
using Frontenis.Domain.Entities;
using Frontenis.Domain.Enums;
using Frontenis.Domain.Exceptions;
using Xunit;

namespace Frontenis.Tests;

/// <summary>El administrador corrige los jugadores de un partido de entrenamiento ya guardado.</summary>
public class TrainingMatchEditTests
{
    private static TrainingSession SessionWithMatch(out TrainingMatch match)
    {
        var s = TrainingSession.Create("Martes", new DateTime(2026, 10, 13), 3, 1);
        match = s.AddMatch(1, 2, 3, 4, 15, 9, BallType.Slow).WithId(1);
        return s;
    }

    [Fact]
    public void Players_are_replaced_and_everything_else_is_kept()
    {
        var session = SessionWithMatch(out var match);
        var playedAt = match.PlayedAtUtc;
        var order = match.Order;

        session.UpdateMatchPlayers(1, 5, 6, 7, 8);

        Assert.Equal(new[] { 5, 6, 7, 8 }, new[] { match.Home1Id, match.Home2Id, match.Away1Id, match.Away2Id });
        Assert.Equal((15, 9), (match.HomeScore, match.AwayScore));
        Assert.Equal(BallType.Slow, match.Ball);
        Assert.Equal(playedAt, match.PlayedAtUtc);
        Assert.Equal(order, match.Order);
    }

    [Fact]
    public void Players_can_be_swapped_between_pairs_or_reused_in_other_positions()
    {
        var session = SessionWithMatch(out var match);
        session.UpdateMatchPlayers(1, 1, 3, 2, 4); // mismos jugadores, parejas distintas
        Assert.Equal(new[] { 1, 3, 2, 4 }, new[] { match.Home1Id, match.Home2Id, match.Away1Id, match.Away2Id });
    }

    [Theory]
    [InlineData(1, 1, 3, 4)] // repetido en la misma pareja
    [InlineData(1, 2, 2, 4)] // repetido entre parejas
    [InlineData(0, 2, 3, 4)] // falta un jugador
    [InlineData(1, 2, 3, -1)]
    public void Invalid_players_are_rejected_and_nothing_changes(int h1, int h2, int a1, int a2)
    {
        var session = SessionWithMatch(out var match);
        Assert.Throws<DomainException>(() => session.UpdateMatchPlayers(1, h1, h2, a1, a2));
        Assert.Equal(new[] { 1, 2, 3, 4 }, new[] { match.Home1Id, match.Home2Id, match.Away1Id, match.Away2Id });
    }

    [Fact]
    public void An_unknown_match_throws_NotFound()
    {
        var session = SessionWithMatch(out _);
        Assert.Throws<NotFoundException>(() => session.UpdateMatchPlayers(99, 1, 2, 3, 4));
    }

    private static (TrainingCommands Commands, FakeTrainings Trainings, FakeUow Uow) Build(int players = 8)
    {
        var fakePlayers = new FakePlayers();
        for (var i = 1; i <= players; i++) fakePlayers.Items.Add(TestHelpers.Player(i, $"J{i}", PlayerPosition.Zaguero));
        var trainings = new FakeTrainings();
        trainings.Items.Add(SessionWithMatch(out _).WithId(1));
        var uow = new FakeUow();
        return (new TrainingCommands(trainings, fakePlayers, new TrainingDetailBuilder(fakePlayers), uow), trainings, uow);
    }

    [Fact]
    public async Task The_service_saves_and_the_standings_use_the_new_players()
    {
        var (commands, _, uow) = Build();

        var detail = await commands.UpdateMatchPlayersAsync(1, 1, new UpdateTrainingMatchPlayersRequest(5, 6, 7, 8));

        Assert.Equal("J5 / J6", detail.Matches.Single().HomeName);
        Assert.Equal("J7 / J8", detail.Matches.Single().AwayName);
        Assert.Equal((15, 9), (detail.Matches.Single().HomeScore, detail.Matches.Single().AwayScore));
        Assert.Equal(BallType.Slow, detail.Matches.Single().Ball);
        Assert.Contains(detail.PlayerStandings, r => r.PlayerIds.Contains(5) && r.Won == 1);
        Assert.DoesNotContain(detail.PlayerStandings, r => r.PlayerIds.Contains(1));
        Assert.Equal(1, uow.Saves);
    }

    [Fact]
    public async Task An_unknown_player_or_match_or_invalid_selection_fails_without_saving()
    {
        var (commands, _, uow) = Build(players: 6);

        await Assert.ThrowsAsync<NotFoundException>(() => commands.UpdateMatchPlayersAsync(1, 1, new(5, 6, 7, 8)));      // 7 y 8 no existen
        await Assert.ThrowsAsync<NotFoundException>(() => commands.UpdateMatchPlayersAsync(1, 99, new(1, 2, 3, 4)));     // partido inexistente
        await Assert.ThrowsAsync<NotFoundException>(() => commands.UpdateMatchPlayersAsync(42, 1, new(1, 2, 3, 4)));     // entreno inexistente
        await Assert.ThrowsAsync<DomainException>(() => commands.UpdateMatchPlayersAsync(1, 1, new(1, 1, 3, 4)));        // repetidos
        await Assert.ThrowsAsync<DomainException>(() => commands.UpdateMatchPlayersAsync(1, 1, new(0, 2, 3, 4)));        // falta uno
        Assert.Equal(0, uow.Saves);
    }
}
