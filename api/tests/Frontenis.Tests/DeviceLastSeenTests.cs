using Frontenis.Application.Services;
using Frontenis.Domain.Entities;
using Xunit;

namespace Frontenis.Tests;

/// <summary>Último acceso de un dispositivo: se anota al abrir la app, como mucho una vez por hora y sin pasar por la unidad de trabajo.</summary>
public class DeviceLastSeenTests
{
    private const string Phone = "phoneAAAA1";

    private sealed class ManualTime : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private static DeviceLinkService Service(BookingWorld w, ManualTime time) => new(w.Hasher, w.Access(), w.Devices, w.Players, w.Uow, time);

    private static void SetLastSeen(DeviceLink link, DateTime? value) =>
        typeof(DeviceLink).GetProperty(nameof(DeviceLink.LastSeenUtc))!.SetValue(link, value);

    [Fact]
    public async Task A_new_link_starts_with_its_last_access_at_the_moment_it_was_linked()
    {
        var w = new BookingWorld();
        await w.Links().LinkAsync(Phone, 1);

        var link = w.Devices.Items.Single();
        Assert.Equal(link.LinkedAtUtc, link.LastSeenUtc);
    }

    [Fact]
    public async Task Opening_the_app_right_after_linking_does_not_write_anything()
    {
        var w = new BookingWorld();
        var time = new ManualTime();
        await w.Links().LinkAsync(Phone, 1);

        await Service(w, time).GetMeAsync(Phone);

        Assert.Equal(0, w.Devices.Touches);
    }

    [Fact]
    public async Task A_link_without_a_last_access_gets_one_on_the_first_opening_and_not_again_within_the_hour()
    {
        var w = new BookingWorld();
        var time = new ManualTime();
        await w.Links().LinkAsync(Phone, 1);
        SetLastSeen(w.Devices.Items.Single(), null);   // un vínculo anterior a esta columna

        var service = Service(w, time);
        await service.GetMeAsync(Phone);
        await service.GetMeAsync(Phone);
        time.Now = time.Now.AddMinutes(59);
        await service.GetMeAsync(Phone);

        Assert.Equal(1, w.Devices.Touches);
        Assert.NotNull(w.Devices.Items.Single().LastSeenUtc);
    }

    [Fact]
    public async Task After_an_hour_the_next_opening_updates_the_last_access()
    {
        var w = new BookingWorld();
        var time = new ManualTime();
        await w.Links().LinkAsync(Phone, 1);
        var service = Service(w, time);

        time.Now = time.Now.AddMinutes(61);
        await service.GetMeAsync(Phone);

        Assert.Equal(1, w.Devices.Touches);
        Assert.Equal(time.Now.UtcDateTime, w.Devices.Items.Single().LastSeenUtc);

        await service.GetMeAsync(Phone);                // misma hora: sin escritura
        Assert.Equal(1, w.Devices.Touches);
    }

    [Fact]
    public async Task The_last_access_is_written_without_saving_the_unit_of_work_so_the_read_cache_is_kept()
    {
        var w = new BookingWorld();
        var time = new ManualTime();
        await w.Links().LinkAsync(Phone, 1);
        var saves = w.Uow.Saves;

        time.Now = time.Now.AddHours(3);
        await Service(w, time).GetMeAsync(Phone);

        Assert.Equal(1, w.Devices.Touches);
        Assert.Equal(saves, w.Uow.Saves);
    }

    [Fact]
    public async Task An_unlinked_device_writes_nothing_and_is_not_an_error()
    {
        var w = new BookingWorld();

        Assert.Null(await Service(w, new ManualTime()).GetMeAsync("phoneBBBB2"));

        Assert.Equal(0, w.Devices.Touches);
    }

    [Fact]
    public async Task The_administrator_list_includes_the_last_access()
    {
        var w = new BookingWorld();
        await w.Links().LinkAsync(Phone, 1);
        var seen = new DateTime(2026, 11, 1, 10, 30, 0, DateTimeKind.Utc);
        SetLastSeen(w.Devices.Items.Single(), seen);
        await w.Links().LinkAsync("phoneBBBB2", 2);
        SetLastSeen(w.Devices.Items.Last(), null);

        var list = await w.LinkAdmin().ListAsync();

        Assert.Equal(seen, list.Single(l => l.PlayerId == 1).LastSeenUtc);
        Assert.Null(list.Single(l => l.PlayerId == 2).LastSeenUtc);
    }
}
