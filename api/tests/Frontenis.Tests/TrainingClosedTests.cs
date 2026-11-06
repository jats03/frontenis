using Frontenis.Application.Dtos;
using Frontenis.Application.Services;
using Frontenis.Domain.Entities;
using Frontenis.Domain.Enums;
using Frontenis.Domain.Exceptions;
using Xunit;

namespace Frontenis.Tests;

/// <summary>Pasado el día del entreno los jugadores ya no registran partidos (el administrador sí, para correcciones).</summary>
public class TrainingClosedTests
{
    private static readonly DateTime TrainingDay = new(2026, 10, 13);
    private static readonly AddTrainingMatchRequest Match = new(1, 2, 3, 4, 15, 10);

    [Theory]
    [InlineData(2026, 10, 13, 0, 0, false)]    // el mismo día, desde primera hora
    [InlineData(2026, 10, 13, 23, 59, false)]  // y hasta el último minuto
    [InlineData(2026, 10, 14, 0, 0, true)]     // desde las 00:00 del día siguiente
    [InlineData(2026, 11, 2, 12, 0, true)]     // semanas después
    [InlineData(2026, 10, 12, 20, 0, false)]   // un entreno de mañana todavía no ha pasado
    public void A_training_is_closed_from_the_start_of_the_next_day(int y, int m, int d, int h, int min, bool closed)
    {
        var session = TrainingSession.Create("Martes", TrainingDay, 3, 1);
        Assert.Equal(closed, session.IsClosedFor(new DateTime(y, m, d, h, min, 0)));
    }

    private static (TrainingCommands Commands, FakeUow Uow, FakeTrainings Trainings, FakeClock Clock) Build(DateTime now)
    {
        var players = new FakePlayers();
        for (var i = 1; i <= 4; i++) players.Items.Add(TestHelpers.Player(i, $"J{i}", PlayerPosition.Zaguero));
        var trainings = new FakeTrainings();
        trainings.Items.Add(TrainingSession.Create("Martes", TrainingDay, 3, 1).WithId(1));
        var uow = new FakeUow();
        var clock = new FakeClock { Now = now };
        return (new TrainingCommands(trainings, players, new TrainingDetailBuilder(players), uow, clock), uow, trainings, clock);
    }

    [Fact]
    public async Task A_player_can_register_matches_during_the_training_day()
    {
        var (commands, uow, _, _) = Build(new DateTime(2026, 10, 13, 23, 30, 0));

        var detail = await commands.AddMatchAsync(1, Match);

        Assert.Single(detail.Matches);
        Assert.Equal(1, uow.Saves);
    }

    [Fact]
    public async Task A_player_cannot_register_a_match_once_the_day_has_passed_and_nothing_is_saved()
    {
        var (commands, uow, trainings, _) = Build(new DateTime(2026, 10, 14, 0, 0, 1));

        var ex = await Assert.ThrowsAsync<DomainException>(() => commands.AddMatchAsync(1, Match));

        Assert.Equal(TrainingSession.ClosedMessage, ex.Message);
        Assert.Empty(trainings.Items.Single().Matches);
        Assert.Equal(0, uow.Saves);
    }

    [Fact]
    public async Task The_administrator_can_still_register_matches_in_a_past_training()
    {
        var (commands, uow, _, _) = Build(new DateTime(2026, 11, 2, 12, 0, 0));

        var detail = await commands.AddMatchAsync(1, Match, byAdmin: true);

        Assert.Single(detail.Matches);
        Assert.Equal(1, uow.Saves);
    }

    [Fact]
    public async Task The_rule_follows_the_clock_so_it_closes_at_midnight()
    {
        var (commands, _, _, clock) = Build(new DateTime(2026, 10, 13, 23, 59, 0));
        await commands.AddMatchAsync(1, Match);

        clock.Now = new DateTime(2026, 10, 14, 0, 0, 0);

        await Assert.ThrowsAsync<DomainException>(() => commands.AddMatchAsync(1, new AddTrainingMatchRequest(1, 3, 2, 4, 15, 12)));
    }
}
