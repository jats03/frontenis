using Frontenis.Application.Dtos;
using Frontenis.Application.Exceptions;
using Frontenis.Domain.Exceptions;
using Xunit;

namespace Frontenis.Tests;

public class BookingDeviceTests
{
    private const string PhoneA = "phoneAAAA1";
    private const string PhoneB = "phoneBBBB2";

    /// <summary>Una franja abierta (lunes 19/10 a las 18:00) con "ahora" = sábado 17/10 a mediodía.</summary>
    private static async Task<(BookingWorld World, int SlotId)> OpenSlotAsync()
    {
        var w = new BookingWorld();
        await w.Schedule().GenerateAsync(new GenerateSlotsRequest(new DateOnly(2026, 10, 19), 1));
        w.Clock.Now = new DateTime(2026, 10, 17, 12, 0, 0);
        return (w, w.Slots.Items.First(s => s.Date == new DateOnly(2026, 10, 19)).Id);
    }

    // ---------- Vínculo del dispositivo ----------

    [Fact]
    public async Task A_device_links_to_one_player_and_a_player_to_one_device()
    {
        var w = new BookingWorld();
        var me = await w.Links().LinkAsync(PhoneA, 1);
        Assert.Equal("Jugador 1", me.Owner.Name);
        Assert.Empty(me.Delegates);
        Assert.Equal(1, w.Uow.Saves);

        Assert.Equal(1, (await w.Links().LinkAsync(PhoneA, 1)).Owner.Id);                    // repetir es inofensivo
        Assert.Equal(1, w.Uow.Saves);
        await Assert.ThrowsAsync<DomainException>(() => w.Links().LinkAsync(PhoneA, 2));      // el móvil ya es de otro
        await Assert.ThrowsAsync<DomainException>(() => w.Links().LinkAsync(PhoneB, 1));      // el jugador ya tiene móvil
        await Assert.ThrowsAsync<DomainException>(() => w.Links().LinkAsync("corto", 2));     // identificador no válido
        await Assert.ThrowsAsync<NotFoundException>(() => w.Links().LinkAsync(PhoneB, 99));   // jugador inexistente
    }

    [Fact]
    public async Task Unlinking_frees_both_sides_and_unknown_devices_are_ignored()
    {
        var w = new BookingWorld();
        await w.Links().LinkAsync(PhoneA, 1);
        Assert.Null(await w.Links().GetMeAsync(PhoneB));
        Assert.Null(await w.Links().GetMeAsync(null));

        await w.Links().UnlinkAsync(PhoneA);
        await w.Links().UnlinkAsync(PhoneB);
        await w.Links().UnlinkAsync("corto");
        Assert.Null(await w.Links().GetMeAsync(PhoneA));
        await w.Links().LinkAsync(PhoneB, 1);                                                  // el jugador vuelve a estar libre
    }

    [Fact]
    public async Task Only_players_without_a_device_can_be_chosen()
    {
        var w = new BookingWorld();
        await w.Links().LinkAsync(PhoneA, 2);
        var available = await w.Links().ListAvailablePlayersAsync();
        Assert.Equal(new[] { 1, 3, 4, 5 }, available.Select(p => p.Id));
    }

    [Fact]
    public async Task Admin_can_list_and_release_devices()
    {
        var w = new BookingWorld();
        await w.Links().LinkAsync(PhoneA, 1);
        var list = Assert.Single(await w.LinkAdmin().ListAsync());
        Assert.Equal("Jugador 1", list.PlayerName);

        await w.LinkAdmin().ReleaseAsync(1);
        Assert.Empty(await w.LinkAdmin().ListAsync());
        await Assert.ThrowsAsync<NotFoundException>(() => w.LinkAdmin().ReleaseAsync(1));
        await w.Links().LinkAsync(PhoneB, 1);                                                  // cambió de móvil
    }

    // ---------- Apadrinados ----------

    [Fact]
    public async Task Admin_manages_delegates_and_the_device_sees_them()
    {
        var w = new BookingWorld();
        var added = await w.DelegateService().AddAsync(new AddPlayerDelegateRequest(1, 2));
        Assert.Equal("Jugador 2", added.DelegateName);
        await w.DelegateService().AddAsync(new AddPlayerDelegateRequest(1, 3));

        await Assert.ThrowsAsync<DomainException>(() => w.DelegateService().AddAsync(new AddPlayerDelegateRequest(1, 2)));  // repetido
        await Assert.ThrowsAsync<DomainException>(() => w.DelegateService().AddAsync(new AddPlayerDelegateRequest(1, 1)));  // a sí mismo
        await Assert.ThrowsAsync<NotFoundException>(() => w.DelegateService().AddAsync(new AddPlayerDelegateRequest(1, 99)));
        Assert.Equal(2, (await w.DelegateService().ListAsync()).Count);

        var me = await w.Links().LinkAsync(PhoneA, 1);
        Assert.Equal(new[] { 2, 3 }, me.Delegates.Select(d => d.Id));

        await w.DelegateService().RemoveAsync(1, 2);
        Assert.Equal(new[] { 3 }, (await w.Links().GetMeAsync(PhoneA))!.Delegates.Select(d => d.Id));
        await Assert.ThrowsAsync<NotFoundException>(() => w.DelegateService().RemoveAsync(1, 2));
    }

    // ---------- Apuntarse desde el dispositivo ----------

    [Fact]
    public async Task A_device_signs_up_its_owner_and_delegates_and_records_who_did_it()
    {
        var (w, slotId) = await OpenSlotAsync();
        await w.DelegateService().AddAsync(new AddPlayerDelegateRequest(1, 2));
        await w.Links().LinkAsync(PhoneA, 1);

        await w.DeviceAttendance().SignUpAsync(PhoneA, slotId, 1);
        var dto = await w.DeviceAttendance().SignUpAsync(PhoneA, slotId, 2);

        Assert.Equal(new[] { 1, 2 }, dto.Attendees.Select(a => a.PlayerId));
        Assert.All(w.Slots.Items.First(s => s.Id == slotId).Attendees, a => Assert.Equal(1, a.RegisteredByPlayerId));
    }

    [Fact]
    public async Task A_device_cannot_sign_up_players_it_does_not_own()
    {
        var (w, slotId) = await OpenSlotAsync();
        await w.Links().LinkAsync(PhoneA, 1);

        await Assert.ThrowsAsync<DomainException>(() => w.DeviceAttendance().SignUpAsync(PhoneA, slotId, 2));
        await Assert.ThrowsAsync<UnauthorizedAppException>(() => w.DeviceAttendance().SignUpAsync(PhoneB, slotId, 1));  // sin vincular
        await Assert.ThrowsAsync<UnauthorizedAppException>(() => w.DeviceAttendance().SignUpAsync(null, slotId, 1));
        Assert.Empty(w.Slots.Items.First(s => s.Id == slotId).Attendees);
    }

    [Fact]
    public async Task A_device_can_withdraw_its_own_and_delegated_players_only()
    {
        var (w, slotId) = await OpenSlotAsync();
        await w.DelegateService().AddAsync(new AddPlayerDelegateRequest(1, 2));
        await w.Links().LinkAsync(PhoneA, 1);
        await w.Attendance().SignUpAsync(slotId, 3);                                           // lo apuntó el administrador
        await w.DeviceAttendance().SignUpAsync(PhoneA, slotId, 2);

        await Assert.ThrowsAsync<DomainException>(() => w.DeviceAttendance().WithdrawAsync(PhoneA, slotId, 3));
        var dto = await w.DeviceAttendance().WithdrawAsync(PhoneA, slotId, 2);
        Assert.Equal(new[] { 3 }, dto.Attendees.Select(a => a.PlayerId));
    }

    [Fact]
    public async Task Rules_of_the_slot_still_apply_to_devices()
    {
        var (w, slotId) = await OpenSlotAsync();
        await w.Links().LinkAsync(PhoneA, 1);
        await w.DeviceAttendance().SignUpAsync(PhoneA, slotId, 1);

        await Assert.ThrowsAsync<DomainException>(() => w.DeviceAttendance().SignUpAsync(PhoneA, slotId, 1));   // ya apuntado
        w.Clock.Now = new DateTime(2026, 10, 19, 18, 0, 0);
        await Assert.ThrowsAsync<DomainException>(() => w.DeviceAttendance().WithdrawAsync(PhoneA, slotId, 1)); // ya empezó
    }
}
