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
    private static readonly DateOnly NextMonday = new(2026, 10, 19);

    // ---------- Generación ----------

    [Fact]
    public async Task Generate_creates_slots_with_the_opening_of_their_week()
    {
        var w = new BookingWorld();
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
        var w = new BookingWorld();
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
        var w = new BookingWorld();
        await Assert.ThrowsAsync<DomainException>(() => w.Schedule().GenerateAsync(new GenerateSlotsRequest(NextMonday, 0)));
        await Assert.ThrowsAsync<DomainException>(() => w.Schedule().GenerateAsync(new GenerateSlotsRequest(NextMonday, 9)));
        await Assert.ThrowsAsync<DomainException>(() => w.Schedule().GenerateAsync(new GenerateSlotsRequest(new DateOnly(2026, 10, 14), 1)));
    }

    [Fact]
    public async Task OpenWeek_opens_pending_slots_and_skips_cancelled_ones()
    {
        var w = new BookingWorld();
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
        var w = new BookingWorld();
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
        var w = new BookingWorld();
        await w.Schedule().GenerateAsync(new GenerateSlotsRequest(NextMonday, 1));
        var slotId = w.Slots.Items.First().Id;

        await Assert.ThrowsAsync<DomainException>(() => w.Attendance().SignUpAsync(slotId, 1));
        var dto = (await w.Queries().ListAsync(NextMonday, NextMonday.AddDays(6), SlotAudience.Admin)).First();
        Assert.Equal(SlotStatus.Pending, dto.Status);
        Assert.False(dto.FewPlayers);
    }

    [Fact]
    public async Task Default_listing_covers_this_week_and_the_next()
    {
        var w = new BookingWorld();
        w.Slots.Items.Add(TrainingSlot.Create(new DateOnly(2026, 10, 12), new TimeOnly(18, 0), new TimeOnly(20, 0), new DateTime(2026, 10, 9, 19, 0, 0)).WithId(1));
        w.Slots.Items.Add(TrainingSlot.Create(new DateOnly(2026, 10, 22), new TimeOnly(18, 0), new TimeOnly(20, 0), new DateTime(2026, 10, 16, 19, 0, 0)).WithId(2));
        w.Slots.Items.Add(TrainingSlot.Create(new DateOnly(2026, 10, 26), new TimeOnly(18, 0), new TimeOnly(20, 0), new DateTime(2026, 10, 23, 19, 0, 0)).WithId(3));

        var list = await w.Queries().ListAsync(null, null, SlotAudience.Admin);

        Assert.Equal(new[] { 1, 2 }, list.Select(s => s.Id));
        await Assert.ThrowsAsync<DomainException>(() => w.Queries().ListAsync(NextMonday, NextMonday.AddDays(-1), SlotAudience.Admin));
        await Assert.ThrowsAsync<DomainException>(() => w.Queries().ListAsync(NextMonday, NextMonday.AddDays(93), SlotAudience.Admin));
    }

    [Fact]
    public async Task CloseWeek_undoes_early_openings_and_leaves_regular_ones()
    {
        var w = new BookingWorld();
        await w.Schedule().GenerateAsync(new GenerateSlotsRequest(NextMonday, 1));
        w.Clock.Now = new DateTime(2026, 10, 14, 12, 0, 0);       // la semana abre el viernes 16
        Assert.Equal(2, (await w.Schedule().OpenWeekAsync(new OpenWeekRequest(NextMonday))).Opened);

        var result = await w.Schedule().CloseWeekAsync(new OpenWeekRequest(NextMonday));

        Assert.Equal(2, result.Closed);
        Assert.All(w.Slots.Items, s => Assert.Equal(new DateTime(2026, 10, 16, 19, 0, 0), s.OpensAt));
        Assert.Equal(0, (await w.Schedule().CloseWeekAsync(new OpenWeekRequest(NextMonday))).Closed);

        w.Clock.Now = new DateTime(2026, 10, 17, 12, 0, 0);       // ya abiertas por su horario
        Assert.Equal(0, (await w.Schedule().CloseWeekAsync(new OpenWeekRequest(NextMonday))).Closed);
    }

    [Fact]
    public async Task Admin_can_close_a_slot_opened_by_mistake_and_the_dto_says_when()
    {
        var w = new BookingWorld();
        await w.Schedule().GenerateAsync(new GenerateSlotsRequest(NextMonday, 1));
        w.Clock.Now = new DateTime(2026, 10, 14, 12, 0, 0);
        var id = w.Slots.Items.First().Id;

        var opened = await w.Admin().OpenNowAsync(id);
        Assert.Equal(SlotStatus.Open, opened.Status);
        Assert.True(opened.CanCloseSignUps);

        var closed = await w.Admin().CloseSignUpsAsync(id);
        Assert.Equal(SlotStatus.Pending, closed.Status);
        Assert.False(closed.CanCloseSignUps);

        w.Clock.Now = new DateTime(2026, 10, 17, 12, 0, 0);       // abierta por su horario: no se puede cerrar
        var regular = (await w.Queries().ListAsync(NextMonday, NextMonday, SlotAudience.Admin)).First(s => s.Id == id);
        Assert.Equal(SlotStatus.Open, regular.Status);
        Assert.False(regular.CanCloseSignUps);
        await Assert.ThrowsAsync<DomainException>(() => w.Admin().CloseSignUpsAsync(id));
    }

    [Fact]
    public async Task Public_listing_shows_this_week_and_open_slots_but_the_admin_sees_everything()
    {
        var w = new BookingWorld();
        w.Clock.Now = new DateTime(2026, 10, 14, 12, 0, 0); // miércoles; la semana del 19 abre el viernes 16
        TrainingSlot Slot(int id, DateOnly date, DateTime opens) =>
            TrainingSlot.Create(date, new TimeOnly(18, 0), new TimeOnly(20, 0), opens).WithId(id);
        w.Slots.Items.Add(Slot(1, new DateOnly(2026, 10, 12), new DateTime(2026, 10, 9, 19, 0, 0)));  // esta semana, pasada
        w.Slots.Items.Add(Slot(2, new DateOnly(2026, 10, 15), new DateTime(2026, 10, 9, 19, 0, 0)));  // esta semana
        w.Slots.Items.Add(Slot(3, new DateOnly(2026, 10, 19), new DateTime(2026, 10, 16, 19, 0, 0))); // próxima, sin abrir
        w.Slots.Items.Add(Slot(4, new DateOnly(2026, 10, 26), new DateTime(2026, 10, 12, 8, 0, 0)));  // otra semana, ya abierta
        w.Slots.Items.Add(Slot(5, new DateOnly(2026, 11, 2), new DateTime(2026, 10, 30, 19, 0, 0)));  // lejana, sin abrir

        var range = (new DateOnly(2026, 10, 12), new DateOnly(2026, 11, 8));
        var publicIds = (await w.Queries().ListAsync(range.Item1, range.Item2)).Select(s => s.Id);
        var adminIds = (await w.Queries().ListAsync(range.Item1, range.Item2, SlotAudience.Admin)).Select(s => s.Id);

        Assert.Equal(new[] { 1, 2, 4 }, publicIds);
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, adminIds);

        w.Clock.Now = new DateTime(2026, 10, 16, 19, 0, 0); // se abre la semana del 19
        Assert.Equal(new[] { 1, 2, 3, 4 }, (await w.Queries().ListAsync(range.Item1, range.Item2)).Select(s => s.Id));
    }

    // ---------- Administración ----------

    [Fact]
    public async Task Admin_can_add_extra_slots_cancel_restore_and_delete()
    {
        var w = new BookingWorld();
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
