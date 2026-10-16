using Frontenis.Domain.Entities;
using Frontenis.Domain.Enums;
using Frontenis.Domain.Exceptions;
using Frontenis.Domain.Services;
using Xunit;

namespace Frontenis.Tests;

public class BookingDomainTests
{
    private static readonly TimeOnly T17 = new(17, 0);
    private static readonly TimeOnly T18 = new(18, 0);
    private static readonly TimeOnly T20 = new(20, 0);

    // Lunes 19/10/2026 de 18:00 a 20:00; se abre el viernes 16/10 a las 19:00.
    private static readonly DateOnly Monday = new(2026, 10, 19);
    private static readonly DateTime Opens = new(2026, 10, 16, 19, 0, 0);

    private static TrainingSlot Slot() => TrainingSlot.Create(Monday, T18, T20, Opens);

    // ---------- Política de apertura ----------

    [Theory]
    [InlineData(2026, 10, 19, 2026, 10, 16)] // lunes → viernes anterior
    [InlineData(2026, 10, 22, 2026, 10, 16)] // jueves de la misma semana
    [InlineData(2026, 10, 23, 2026, 10, 16)] // viernes: se abre una semana antes, con el resto de la semana
    [InlineData(2026, 10, 25, 2026, 10, 16)] // domingo
    [InlineData(2026, 10, 12, 2026, 10, 9)]
    public void Weekly_policy_opens_the_whole_week_on_the_previous_friday(int y, int m, int d, int oy, int om, int od)
    {
        var policy = new WeeklyOpeningPolicy(DayOfWeek.Friday, new TimeOnly(19, 0));
        Assert.Equal(new DateTime(oy, om, od, 19, 0, 0), policy.OpensAtFor(new DateOnly(y, m, d)));
    }

    // ---------- Generador ----------

    [Fact]
    public void Generator_repeats_active_templates_on_their_day_and_ignores_inactive_ones()
    {
        var mon1 = SlotTemplate.Create(DayOfWeek.Monday, T17, T18).WithId(1);
        var mon2 = SlotTemplate.Create(DayOfWeek.Monday, T18, T20).WithId(2);
        var fri = SlotTemplate.Create(DayOfWeek.Friday, new TimeOnly(19, 0), new TimeOnly(22, 0)).WithId(3);
        var off = SlotTemplate.Create(DayOfWeek.Thursday, T18, T20).WithId(4);
        off.Update(DayOfWeek.Thursday, T18, T20, active: false);

        var planned = new WeeklySlotGenerator().Generate(new[] { fri, off, mon2, mon1 }, Monday, Monday.AddDays(13));

        Assert.Equal(6, planned.Count);
        Assert.Equal(new[] { 1, 2, 3, 1, 2, 3 }, planned.Select(p => p.TemplateId));
        Assert.Equal(Monday, planned[0].Date);
        Assert.Equal(Monday.AddDays(4), planned[2].Date);
    }

    [Fact]
    public void Weeks_start_on_monday()
    {
        Assert.Equal(Monday, Weeks.MondayOf(new DateOnly(2026, 10, 25))); // domingo
        Assert.Equal(Monday, Weeks.MondayOf(Monday));
    }

    // ---------- Franja ----------

    [Fact]
    public void Times_must_be_consistent()
    {
        Assert.Throws<DomainException>(() => SlotTemplate.Create(DayOfWeek.Monday, T20, T18));
        Assert.Throws<DomainException>(() => TrainingSlot.Create(Monday, T18, T18, Opens));
        // la apertura no puede ser posterior al inicio
        Assert.Throws<DomainException>(() => TrainingSlot.Create(Monday, T18, T20, new DateTime(2026, 10, 19, 18, 1, 0)));
    }

    [Fact]
    public void Slot_is_open_between_opening_and_start()
    {
        var s = Slot();
        Assert.False(s.IsOpen(Opens.AddMinutes(-1)));
        Assert.True(s.IsOpen(Opens));
        Assert.True(s.IsOpen(new DateTime(2026, 10, 19, 17, 59, 0)));
        Assert.False(s.IsOpen(new DateTime(2026, 10, 19, 18, 0, 0)));
    }

    [Fact]
    public void Status_goes_from_pending_to_open_to_closed_and_cancelled_wins()
    {
        var s = Slot();
        Assert.Equal(SlotStatus.Pending, s.StatusAt(Opens.AddMinutes(-1)));
        Assert.Equal(SlotStatus.Open, s.StatusAt(Opens));
        Assert.Equal(SlotStatus.Closed, s.StatusAt(new DateTime(2026, 10, 19, 18, 0, 0)));
        s.Cancel();
        Assert.Equal(SlotStatus.Cancelled, s.StatusAt(Opens));
    }

    [Fact]
    public void Signing_up_requires_an_open_slot_and_rejects_duplicates()
    {
        var s = Slot();
        Assert.Throws<DomainException>(() => s.SignUp(1, Opens.AddMinutes(-1)));              // aún sin abrir
        s.SignUp(1, Opens);
        Assert.Throws<DomainException>(() => s.SignUp(1, Opens));                              // repetido
        Assert.Throws<DomainException>(() => s.SignUp(0, Opens));                              // jugador no válido
        Assert.Throws<DomainException>(() => s.SignUp(2, new DateTime(2026, 10, 19, 18, 0, 0))); // ya empezada
        s.Cancel();
        Assert.Throws<DomainException>(() => s.SignUp(2, Opens));                              // cancelada
        s.Restore();
        s.SignUp(2, Opens);
        Assert.Equal(2, s.Attendees.Count);
    }

    [Fact]
    public void Slot_has_an_attendee_limit()
    {
        var s = Slot();
        for (var i = 1; i <= TrainingSlot.MaxAttendees; i++) s.SignUp(i, Opens);
        Assert.Throws<DomainException>(() => s.SignUp(TrainingSlot.MaxAttendees + 1, Opens));
    }

    [Fact]
    public void Withdraw_works_until_the_slot_starts()
    {
        var s = Slot();
        s.SignUp(1, Opens);
        Assert.Throws<NotFoundException>(() => s.Withdraw(9, Opens));
        Assert.Throws<DomainException>(() => s.Withdraw(1, new DateTime(2026, 10, 19, 18, 0, 0)));
        s.Withdraw(1, Opens.AddHours(1));
        Assert.Empty(s.Attendees);
    }

    [Fact]
    public void Few_players_warning_only_applies_to_open_slots_below_the_minimum()
    {
        var s = Slot();
        Assert.False(s.HasFewPlayers(4, Opens.AddMinutes(-1)));  // aún sin abrir
        Assert.True(s.HasFewPlayers(4, Opens));                  // 0 apuntados
        for (var i = 1; i <= 3; i++) s.SignUp(i, Opens);
        Assert.True(s.HasFewPlayers(4, Opens));
        s.SignUp(4, Opens);
        Assert.False(s.HasFewPlayers(4, Opens));                 // 4: sin aviso
        s.Withdraw(4, Opens);
        s.Cancel();
        Assert.False(s.HasFewPlayers(4, Opens));                 // cancelada: sin aviso
    }

    [Fact]
    public void OpenNow_brings_the_opening_forward_and_is_idempotent()
    {
        var s = Slot();
        var now = new DateTime(2026, 10, 15, 12, 0, 0);
        Assert.True(s.OpenNow(now));
        Assert.Equal(now, s.OpensAt);
        Assert.True(s.IsOpen(now));
        Assert.False(s.OpenNow(now.AddHours(1)));                // ya abierta: no cambia
        Assert.Equal(now, s.OpensAt);
        Assert.Throws<DomainException>(() => s.OpenNow(new DateTime(2026, 10, 19, 18, 0, 0)));
        s.Cancel();
        Assert.Throws<DomainException>(() => s.OpenNow(now));
    }

    [Fact]
    public void Reschedule_changes_the_slot_but_validates_it()
    {
        var s = Slot();
        s.Reschedule(Monday.AddDays(1), T17, T18, Opens);
        Assert.Equal(Monday.AddDays(1), s.Date);
        Assert.Throws<DomainException>(() => s.Reschedule(Monday, T20, T18, Opens));
        Assert.Throws<DomainException>(() => s.Reschedule(Monday, T18, T20, Monday.ToDateTime(T20)));
    }
}
