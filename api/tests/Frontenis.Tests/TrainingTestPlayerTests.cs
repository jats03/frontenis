using Frontenis.Application.Dtos;
using Frontenis.Application.Services;
using Frontenis.Domain.Entities;
using Frontenis.Domain.Enums;
using Frontenis.Domain.Exceptions;
using Frontenis.Domain.Models;
using Frontenis.Domain.Services;
using Xunit;

namespace Frontenis.Tests;

/// <summary>El «Jugador de prueba» (id 0) ocupa un hueco en un partido de entrenamiento pero no puntúa ni cuenta en nada.</summary>
public class TrainingTestPlayerTests
{
    private static (TrainingCommands Commands, FakePlayers Players, FakeUow Uow) Build()
    {
        var players = new FakePlayers();
        for (var i = 1; i <= 5; i++) players.Items.Add(TestHelpers.Player(i, $"J{i}", PlayerPosition.Zaguero));
        var trainings = new FakeTrainings();
        trainings.Items.Add(TrainingSession.Create("Martes", new DateTime(2026, 10, 13), 3, 1).WithId(1));
        var uow = new FakeUow();
        var clock = new FakeClock { Now = new DateTime(2026, 10, 13, 20, 0, 0) };
        return (new TrainingCommands(trainings, players, new TrainingDetailBuilder(players), uow, clock), players, uow);
    }

    [Fact]
    public async Task The_test_player_can_fill_a_slot_and_is_shown_by_name_with_id_zero()
    {
        var (commands, players, _) = Build();

        var detail = await commands.AddMatchAsync(1, new AddTrainingMatchRequest(1, 0, 3, 4, 15, 9));

        var m = detail.Matches.Single();
        Assert.Equal(("J1 / Jugador de prueba", "J3 / J4"), (m.HomeName, m.AwayName));
        Assert.Equal(new[] { 1, 0 }, m.HomePlayerIds); // se devuelve con el mismo id con el que se elige
        var wildcard = Assert.Single(players.Items, p => p.IsReserve);
        Assert.Equal(Player.TestName, wildcard.Name);
    }

    [Fact]
    public async Task The_test_player_is_created_once_and_reused()
    {
        var (commands, players, _) = Build();

        await commands.AddMatchAsync(1, new AddTrainingMatchRequest(1, 0, 3, 4, 15, 9));
        await commands.AddMatchAsync(1, new AddTrainingMatchRequest(2, 3, 5, 0, 15, 9));

        Assert.Single(players.Items, p => p.IsReserve);
    }

    [Fact]
    public async Task The_test_player_has_no_standings_row_but_the_others_count_the_match()
    {
        var (commands, _, _) = Build();

        var detail = await commands.AddMatchAsync(1, new AddTrainingMatchRequest(1, 0, 3, 4, 15, 9));

        Assert.Equal(new[] { 1, 3, 4 }, detail.PlayerStandings.SelectMany(r => r.PlayerIds).OrderBy(x => x).ToArray());
        Assert.Contains(detail.PlayerStandings, r => r.PlayerIds.Contains(1) && r.Won == 1 && r.ClassificationPoints == 3);
        Assert.Contains(detail.PlayerStandings, r => r.PlayerIds.Contains(3) && r.Lost == 1);
        // por parejas: la pareja con el jugador de prueba no sale; la rival sí
        var pair = Assert.Single(detail.PairStandings);
        Assert.Equal(new[] { 3, 4 }, pair.PlayerIds);
    }

    [Fact]
    public async Task Only_one_test_player_fits_in_a_match()
    {
        var (commands, _, _) = Build();

        await Assert.ThrowsAsync<DomainException>(() => commands.AddMatchAsync(1, new AddTrainingMatchRequest(0, 0, 3, 4, 15, 9)));
    }

    [Fact]
    public async Task An_unknown_player_is_still_rejected()
    {
        var (commands, _, _) = Build();

        await Assert.ThrowsAsync<NotFoundException>(() => commands.AddMatchAsync(1, new AddTrainingMatchRequest(1, 99, 3, 4, 15, 9)));
    }

    [Fact]
    public async Task The_test_player_can_be_put_in_or_taken_out_when_correcting_a_match()
    {
        var (commands, players, _) = Build();
        var added = await commands.AddMatchAsync(1, new AddTrainingMatchRequest(1, 2, 3, 4, 15, 9));
        var matchId = added.Matches.Single().Id; // en el doble de pruebas no hay base de datos que le dé un id

        var withTest = await commands.UpdateMatchAsync(1, matchId, new UpdateTrainingMatchRequest(1, 0, 3, 4, 15, 9, BallType.PreOlympic));
        Assert.Equal("J1 / Jugador de prueba", withTest.Matches.Single().HomeName);
        Assert.Single(players.Items, p => p.IsReserve);

        var withoutTest = await commands.UpdateMatchAsync(1, matchId, new UpdateTrainingMatchRequest(1, 2, 3, 4, 15, 9, BallType.PreOlympic));
        Assert.Equal("J1 / J2", withoutTest.Matches.Single().HomeName);
        Assert.Equal(new[] { 1, 2 }, withoutTest.Matches.Single().HomePlayerIds);
    }

    [Fact]
    public void Standings_can_ignore_wildcard_players()
    {
        var games = new[] { new TrainingGame(1, 9, 3, 4, 15, 9) };

        var players = TrainingStandings.ByPlayer(games, 3, 1, new HashSet<int> { 9 });
        var pairs = TrainingStandings.ByPair(games, 3, 1, new HashSet<int> { 9 });

        Assert.DoesNotContain(players, r => r.Player1Id == 9);
        Assert.Equal(3, players.Count);
        Assert.Single(pairs);
        Assert.Equal((3, 4), (pairs[0].Player1Id, pairs[0].Player2Id));
    }
}
