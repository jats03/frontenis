using Frontenis.Application.Interfaces.Repositories;
using Frontenis.Application.Dtos;
using Frontenis.Application.Models;
using Frontenis.Application.Services;
using Frontenis.Domain.Entities;
using Frontenis.Domain.Enums;
using Frontenis.Domain.Exceptions;
using Frontenis.Domain.Interfaces;
using Frontenis.Domain.Services;
using Xunit;

namespace Frontenis.Tests;

public class BookingServiceTests
{
    // ---------- Dobles de prueba en memoria ----------

    private sealed class FakeClock : IClock { public DateTime Now { get; set; } }

    private sealed class FakeUow : IUnitOfWork
    {
        public int Saves { get; private set; }
        public Task SaveChangesAsync(CancellationToken ct = default) { Saves++; return Task.CompletedTask; }
    }

    private sealed class FakeSlots : ISlotRepository
    {
        private int _next = 1;
        public List<TrainingSlot> Items { get; } = new();

        public Task<IReadOnlyList<TrainingSlot>> ListAsync(DateOnly from, DateOnly to, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<TrainingSlot>>(Items.Where(s => s.Date >= from && s.Date <= to)
                .OrderBy(s => s.Date).ThenBy(s => s.Start).ToList());
        public Task<IReadOnlyList<TrainingSlot>> ListForUpdateAsync(DateOnly from, DateOnly to, CancellationToken ct = default) =>
            ListAsync(from, to, ct);
        public Task<TrainingSlot?> GetAsync(int id, CancellationToken ct = default) =>
            Task.FromResult(Items.FirstOrDefault(s => s.Id == id));
        public Task<IReadOnlyCollection<GeneratedSlotKey>> GeneratedKeysAsync(DateOnly from, DateOnly to, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyCollection<GeneratedSlotKey>>(Items
                .Where(s => s.TemplateId != null && s.Date >= Weeks.MondayOf(from) && s.Date <= to)
                .Select(s => new GeneratedSlotKey(s.TemplateId!.Value, Weeks.MondayOf(s.Date))).ToList());
        public Task AddAsync(TrainingSlot slot, CancellationToken ct = default) { slot.WithId(_next++); Items.Add(slot); return Task.CompletedTask; }
        public Task AddRangeAsync(IEnumerable<TrainingSlot> slots, CancellationToken ct = default)
        {
            foreach (var s in slots) { s.WithId(_next++); Items.Add(s); }
            return Task.CompletedTask;
        }
        public void Remove(TrainingSlot slot) => Items.Remove(slot);
    }

    private sealed class FakeTemplates : ISlotTemplateRepository
    {
        public List<SlotTemplate> Items { get; } = new();
        public Task<IReadOnlyList<SlotTemplate>> ListAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<SlotTemplate>>(Items.ToList());
        public Task<SlotTemplate?> GetAsync(int id, CancellationToken ct = default) =>
            Task.FromResult(Items.FirstOrDefault(t => t.Id == id));
        public Task AddAsync(SlotTemplate template, CancellationToken ct = default) { template.WithId(Items.Count + 1); Items.Add(template); return Task.CompletedTask; }
        public void Remove(SlotTemplate template) => Items.Remove(template);
    }

    private sealed class FakePlayers : IPlayerRepository
    {
        public List<Player> Items { get; } = new();
        public Task<IReadOnlyList<Player>> ListAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Player>>(Items.ToList());
        public Task<IReadOnlyList<Player>> GetByIdsAsync(IReadOnlyCollection<int> ids, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Player>>(Items.Where(p => ids.Contains(p.Id)).ToList());
        public Task<Player?> GetByIdAsync(int id, CancellationToken ct = default) => Task.FromResult(Items.FirstOrDefault(p => p.Id == id));
        public Task AddAsync(Player player, CancellationToken ct = default) { Items.Add(player); return Task.CompletedTask; }
        public void Remove(Player player) => Items.Remove(player);
        public Task<bool> IsInUseAsync(int playerId, CancellationToken ct = default) => Task.FromResult(false);
    }

    private sealed class World
    {
        public FakeClock Clock { get; } = new() { Now = new DateTime(2026, 10, 15, 10, 0, 0) };
        public FakeUow Uow { get; } = new();
        public FakeSlots Slots { get; } = new();
        public FakeTemplates Templates { get; } = new();
        public FakePlayers Players { get; } = new();
        public BookingOptions Options { get; } = new();
        public IOpeningPolicy Opening { get; } = new WeeklyOpeningPolicy(DayOfWeek.Friday, new TimeOnly(19, 0));

        public World()
        {
            Templates.AddAsync(SlotTemplate.Create(DayOfWeek.Monday, new TimeOnly(18, 0), new TimeOnly(20, 0)));
            Templates.AddAsync(SlotTemplate.Create(DayOfWeek.Friday, new TimeOnly(19, 0), new TimeOnly(22, 0)));
            for (var i = 1; i <= 5; i++) Players.Items.Add(TestHelpers.Player(i, $"Jugador {i}", PlayerPosition.Delantero));
        }

        public SlotScheduleService Schedule() =>
            new(Slots, Templates, new WeeklySlotGenerator(), Opening, Clock, Uow);
        public SlotDtoBuilder Builder() => new(Players, Clock, Options);
        public AttendanceCommands Attendance() => new(Slots, Players, Builder(), Clock, Uow);
        public SlotAdminCommands Admin() => new(Slots, Builder(), Opening, Clock, Uow);
        public SlotQueries Queries() => new(Slots, Builder(), Clock);
    }

    private static readonly DateOnly NextMonday = new(2026, 10, 19);

    // ---------- Generación ----------

    [Fact]
    public async Task Generate_creates_slots_with_the_opening_of_their_week()
    {
        var w = new World();
        var result = await w.Schedule().GenerateAsync(new GenerateSlotsRequest(NextMonday, 2));

        Assert.Equal(4, result.Created);
        Assert.Equal(1, w.Uow.Saves);
        var first = w.Slots.Items.Single(s => s.Date == NextMonday);
        Assert.Equal(new DateTime(2026, 10, 16, 19, 0, 0), first.OpensAt);
        var secondWeek = w.Slots.Items.Single(s => s.Date == NextMonday.AddDays(7));
        Assert.Equal(new DateTime(2026, 10, 23, 19, 0, 0), secondWeek.OpensAt);
    }

    [Fact]
    public async Task Generate_is_idempotent_even_if_a_slot_was_moved_or_cancelled()
    {
        var w = new World();
        await w.Schedule().GenerateAsync(new GenerateSlotsRequest(NextMonday, 2));
        var monday = w.Slots.Items.First(s => s.Date == NextMonday);
        monday.Reschedule(NextMonday.AddDays(1), new TimeOnly(18, 0), new TimeOnly(20, 0), monday.OpensAt);
        w.Slots.Items.First(s => s.Date == NextMonday.AddDays(4)).Cancel();

        var again = await w.Schedule().GenerateAsync(new GenerateSlotsRequest(NextMonday, 2));

        Assert.Equal(0, again.Created);
        Assert.Equal(4, w.Slots.Items.Count);
        Assert.Equal(1, w.Uow.Saves);
    }

    [Fact]
    public async Task Generate_validates_the_request()
    {
        var w = new World();
        await Assert.ThrowsAsync<DomainException>(() => w.Schedule().GenerateAsync(new GenerateSlotsRequest(NextMonday, 0)));
        await Assert.ThrowsAsync<DomainException>(() => w.Schedule().GenerateAsync(new GenerateSlotsRequest(NextMonday, 9)));
        await Assert.ThrowsAsync<DomainException>(() => w.Schedule().GenerateAsync(new GenerateSlotsRequest(new DateOnly(2026, 10, 14), 1)));
    }

    [Fact]
    public async Task OpenWeek_opens_pending_slots_and_skips_cancelled_ones()
    {
        var w = new World();
        await w.Schedule().GenerateAsync(new GenerateSlotsRequest(NextMonday, 1));
        w.Slots.Items.Single(s => s.Date == NextMonday.AddDays(4)).Cancel();

        var result = await w.Schedule().OpenWeekAsync(new OpenWeekRequest(NextMonday.AddDays(2)));

        Assert.Equal(1, result.Opened);
        Assert.Equal(w.Clock.Now, w.Slots.Items.Single(s => s.Date == NextMonday).OpensAt);
        Assert.Equal(0, (await w.Schedule().OpenWeekAsync(new OpenWeekRequest(NextMonday))).Opened);
    }

    // ---------- Apuntarse y consultas ----------

    [Fact]
    public async Task Signing_up_shows_names_and_the_few_players_warning_until_four()
    {
        var w = new World();
        await w.Schedule().GenerateAsync(new GenerateSlotsRequest(NextMonday, 1));
        var slotId = w.Slots.Items.Single(s => s.Date == NextMonday).Id;
        w.Clock.Now = new DateTime(2026, 10, 17, 12, 0, 0);

        await Assert.ThrowsAsync<NotFoundException>(() => w.Attendance().SignUpAsync(slotId, 99));
        for (var p = 1; p <= 3; p++) await w.Attendance().SignUpAsync(slotId, p);

        var dto = Assert.Single(await w.Queries().ListAsync(NextMonday, NextMonday));
        Assert.Equal(SlotStatus.Open, dto.Status);
        Assert.True(dto.FewPlayers);
        Assert.Equal(3, dto.AttendeeCount);
        Assert.Equal("Jugador 1", dto.Attendees[0].Name);

        await w.Attendance().SignUpAsync(slotId, 4);
        Assert.False((await w.Queries().ListAsync(NextMonday, NextMonday)).Single().FewPlayers);

        var afterWithdraw = await w.Attendance().WithdrawAsync(slotId, 4);
        Assert.True(afterWithdraw.FewPlayers);
    }

    [Fact]
    public async Task Slots_not_yet_open_are_listed_but_cannot_be_joined()
    {
        var w = new World();
        await w.Schedule().GenerateAsync(new GenerateSlotsRequest(NextMonday, 1));
        var slotId = w.Slots.Items.First().Id;

        await Assert.ThrowsAsync<DomainException>(() => w.Attendance().SignUpAsync(slotId, 1));
        var dto = (await w.Queries().ListAsync(NextMonday, NextMonday.AddDays(6))).First();
        Assert.Equal(SlotStatus.Pending, dto.Status);
        Assert.False(dto.FewPlayers);
    }

    [Fact]
    public async Task Default_listing_covers_this_week_and_the_next()
    {
        var w = new World();
        w.Slots.Items.Add(TrainingSlot.Create(new DateOnly(2026, 10, 12), new TimeOnly(18, 0), new TimeOnly(20, 0), new DateTime(2026, 10, 9, 19, 0, 0)).WithId(1));
        w.Slots.Items.Add(TrainingSlot.Create(new DateOnly(2026, 10, 22), new TimeOnly(18, 0), new TimeOnly(20, 0), new DateTime(2026, 10, 16, 19, 0, 0)).WithId(2));
        w.Slots.Items.Add(TrainingSlot.Create(new DateOnly(2026, 10, 26), new TimeOnly(18, 0), new TimeOnly(20, 0), new DateTime(2026, 10, 23, 19, 0, 0)).WithId(3));

        var list = await w.Queries().ListAsync(null, null);

        Assert.Equal(new[] { 1, 2 }, list.Select(s => s.Id));
        await Assert.ThrowsAsync<DomainException>(() => w.Queries().ListAsync(NextMonday, NextMonday.AddDays(-1)));
        await Assert.ThrowsAsync<DomainException>(() => w.Queries().ListAsync(NextMonday, NextMonday.AddDays(93)));
    }

    // ---------- Administración ----------

    [Fact]
    public async Task Admin_can_add_extra_slots_cancel_restore_and_delete()
    {
        var w = new World();
        var dto = await w.Admin().CreateAsync(new SaveSlotRequest(new DateOnly(2026, 10, 21), new TimeOnly(19, 0), new TimeOnly(21, 0)));
        Assert.Equal(new DateTime(2026, 10, 16, 19, 0, 0), dto.OpensAt);   // regla de apertura por defecto
        Assert.Null(dto.TemplateId);

        Assert.Equal(SlotStatus.Cancelled, (await w.Admin().CancelAsync(dto.Id)).Status);
        Assert.Equal(SlotStatus.Pending, (await w.Admin().RestoreAsync(dto.Id)).Status);

        var opened = await w.Admin().OpenNowAsync(dto.Id);
        Assert.Equal(w.Clock.Now, opened.OpensAt);
        Assert.Equal(SlotStatus.Open, opened.Status);

        await w.Admin().DeleteAsync(dto.Id);
        Assert.Empty(w.Slots.Items);
        await Assert.ThrowsAsync<NotFoundException>(() => w.Admin().DeleteAsync(dto.Id));
    }
}
