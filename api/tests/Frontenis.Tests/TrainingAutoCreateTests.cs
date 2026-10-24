using Frontenis.Application.Interfaces.Repositories;
using Frontenis.Application.Services;
using Frontenis.Domain.Entities;
using Xunit;

namespace Frontenis.Tests;

internal sealed class FakeTrainings : ITrainingRepository
{
    public List<TrainingSession> Items { get; } = new();
    public HashSet<DateTime> Runs { get; } = new();
    public Task<IReadOnlyList<TrainingSession>> ListAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<TrainingSession>>(Items.ToList());
    public Task<TrainingSession?> GetAsync(int id, CancellationToken ct = default) => Task.FromResult(Items.FirstOrDefault(s => s.Id == id));
    public Task<bool> ExistsOnAsync(DateTime date, CancellationToken ct = default) => Task.FromResult(Items.Any(s => s.SessionDate == date.Date));
    public Task<bool> AutoRunExistsAsync(DateTime date, CancellationToken ct = default) => Task.FromResult(Runs.Contains(date.Date));
    public Task AddAutoRunAsync(DateTime date, CancellationToken ct = default) { Runs.Add(date.Date); return Task.CompletedTask; }
    public Task AddAsync(TrainingSession session, CancellationToken ct = default) { Items.Add(session); return Task.CompletedTask; }
    public void Remove(TrainingSession session) => Items.Remove(session);
}

public class TrainingAutoCreateTests
{
    private static (TrainingAutoCreator, FakeTrainings, FakeClock, FakeUow) Setup(DateTime now)
    {
        var repo = new FakeTrainings();
        var clock = new FakeClock { Now = now };
        var uow = new FakeUow();
        return (new TrainingAutoCreator(repo, clock, uow), repo, clock, uow);
    }

    // 2026-10-13 es martes; 2026-10-15, jueves; 2026-10-14, miércoles.
    [Fact]
    public async Task Creates_the_empty_training_on_tuesday_from_eight_with_the_default_name()
    {
        var (svc, repo, _, uow) = Setup(new DateTime(2026, 10, 13, 8, 0, 0));
        Assert.True(await svc.EnsureTodayAsync());
        var s = Assert.Single(repo.Items);
        Assert.Equal("Martes, 13 de octubre", s.Name);
        Assert.Equal(new DateTime(2026, 10, 13), s.SessionDate);
        Assert.Empty(s.Matches);
        Assert.Equal((3, 0), (s.PointsPerWin, s.PointsPerLoss));
        Assert.Equal(1, uow.Saves);
    }

    [Fact]
    public async Task Creates_it_on_thursday_and_later_in_the_day_if_the_server_was_asleep()
    {
        var (svc, repo, _, _) = Setup(new DateTime(2026, 10, 15, 17, 42, 0));
        Assert.True(await svc.EnsureTodayAsync());
        Assert.Equal("Jueves, 15 de octubre", repo.Items.Single().Name);
    }

    [Theory]
    [InlineData(2026, 10, 13, 7, 59)]   // martes antes de las 8:00
    [InlineData(2026, 10, 14, 12, 0)]   // miércoles
    [InlineData(2026, 10, 12, 12, 0)]   // lunes
    [InlineData(2026, 10, 17, 12, 0)]   // sábado
    [InlineData(2026, 10, 18, 12, 0)]   // domingo
    public async Task Does_nothing_on_other_days_or_before_eight(int y, int m, int d, int h, int min)
    {
        var (svc, repo, _, uow) = Setup(new DateTime(y, m, d, h, min, 0));
        Assert.False(await svc.EnsureTodayAsync());
        Assert.Empty(repo.Items);
        Assert.Equal(0, uow.Saves);
    }

    [Fact]
    public async Task Runs_only_once_per_day_even_if_the_training_is_deleted()
    {
        var (svc, repo, clock, _) = Setup(new DateTime(2026, 10, 13, 9, 5, 0));
        Assert.True(await svc.EnsureTodayAsync());
        Assert.False(await svc.EnsureTodayAsync());
        repo.Items.Clear();                                      // el administrador lo borra
        clock.Now = new DateTime(2026, 10, 13, 20, 0, 0);
        Assert.False(await svc.EnsureTodayAsync());              // no se vuelve a crear ese día
        Assert.Empty(repo.Items);
        clock.Now = new DateTime(2026, 10, 15, 9, 0, 0);         // pero el jueves sí
        Assert.True(await svc.EnsureTodayAsync());
    }

    [Fact]
    public async Task Does_not_duplicate_a_training_the_admin_already_created_that_day()
    {
        var (svc, repo, _, _) = Setup(new DateTime(2026, 10, 13, 10, 0, 0));
        repo.Items.Add(TrainingSession.Create("Mi entreno", new DateTime(2026, 10, 13), 3, 0));
        Assert.False(await svc.EnsureTodayAsync());
        Assert.Single(repo.Items);
        Assert.Contains(new DateTime(2026, 10, 13), repo.Runs);
    }

    [Theory]
    [InlineData("2026-10-13T07:00", "2026-10-13T08:00")]   // martes antes de las 8
    [InlineData("2026-10-13T08:00", "2026-10-13T13:00")]   // cada 5 horas: 8, 13, 18, 23
    [InlineData("2026-10-13T13:00", "2026-10-13T18:00")]
    [InlineData("2026-10-13T20:30", "2026-10-13T23:00")]
    [InlineData("2026-10-13T23:00", "2026-10-15T08:00")]   // tras el martes, el jueves
    [InlineData("2026-10-14T12:00", "2026-10-15T08:00")]   // miércoles
    [InlineData("2026-10-15T23:30", "2026-10-20T08:00")]   // tras el jueves, el martes siguiente
    [InlineData("2026-10-17T10:00", "2026-10-20T08:00")]   // sábado
    public void Next_check_follows_the_every_five_hours_schedule(string now, string expected) =>
        Assert.Equal(DateTime.Parse(expected), TrainingAutoCreator.NextCheck(DateTime.Parse(now)));

    [Theory]
    [InlineData(2026, 1, 1, "Jueves, 1 de enero")]
    [InlineData(2026, 12, 29, "Martes, 29 de diciembre")]
    [InlineData(2026, 9, 3, "Jueves, 3 de septiembre")]
    public void The_default_name_matches_the_web_format(int y, int m, int d, string expected) =>
        Assert.Equal(expected, TrainingAutoCreator.DefaultName(new DateTime(y, m, d)));
}
