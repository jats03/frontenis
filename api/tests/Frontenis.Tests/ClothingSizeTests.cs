using Frontenis.Application.Dtos;
using Frontenis.Application.Exceptions;
using Frontenis.Application.Services;
using Frontenis.Domain.Entities;
using Frontenis.Domain.Enums;
using Frontenis.Domain.Exceptions;
using Frontenis.Domain.Services;
using Xunit;

namespace Frontenis.Tests;

/// <summary>Tallas de ropa (camiseta y pantalón) de cada jugador y de sus apadrinados.</summary>
public class ClothingSizeTests
{
    private const string PhoneA = "phoneAAAA1";

    private static Player NewPlayer() => new("Ana", PlayerPosition.Zaguero);

    [Theory]
    [InlineData("m", "M")]
    [InlineData(" xl ", "XL")]
    [InlineData("10", "10")]
    [InlineData("3xl", "3XL")]
    public void Sizes_are_cleaned_and_kept(string input, string expected)
    {
        var p = NewPlayer();
        p.SetClothingSizes(input, input);
        Assert.Equal((expected, expected), (p.TopSize, p.BottomSize));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void An_empty_size_means_not_filled_in(string? empty)
    {
        var p = NewPlayer();
        p.SetClothingSizes("M", "L");
        p.SetClothingSizes(empty, "S");
        Assert.Null(p.TopSize);
        Assert.Equal("S", p.BottomSize);
    }

    [Theory]
    [InlineData("XXXL")]
    [InlineData("7")]
    [InlineData("grande")]
    public void An_unknown_size_is_rejected_and_nothing_changes(string bad)
    {
        var p = NewPlayer();
        p.SetClothingSizes("M", "L");
        Assert.Throws<DomainException>(() => p.SetClothingSizes("S", bad));
        Assert.Throws<DomainException>(() => p.SetClothingSizes(bad, "S"));
        Assert.Equal(("M", "L"), (p.TopSize, p.BottomSize));
    }

    [Fact]
    public void Every_allowed_size_fits_the_column()
    {
        Assert.All(ClothingSizes.Allowed, s => Assert.InRange(s.Length, 1, ClothingSizes.MaxLength));
    }

    private static async Task<(BookingWorld World, ClothingSizeService Service)> Linked(params int[] delegateIds)
    {
        var w = new BookingWorld();
        await w.Links().LinkAsync(PhoneA, 1);
        foreach (var d in delegateIds) await w.DelegateService().AddAsync(new AddPlayerDelegateRequest(1, d));
        return (w, new ClothingSizeService(w.Access(), w.Players, w.Uow));
    }

    [Fact]
    public async Task A_device_lists_and_saves_its_own_sizes_and_those_of_its_delegates()
    {
        var (w, service) = await Linked(2, 3);
        var start = w.Uow.Saves;

        var before = await service.ListForDeviceAsync(PhoneA);
        Assert.Equal(new[] { 1, 2, 3 }, before.Select(r => r.PlayerId));
        Assert.All(before, r => Assert.True(r.TopSize is null && r.BottomSize is null));

        var saved = await service.SaveForDeviceAsync(PhoneA, 2, new SaveClothingSizeRequest("12", "10"));
        Assert.Equal((2, "12", "10"), (saved.PlayerId, saved.TopSize, saved.BottomSize));
        await service.SaveForDeviceAsync(PhoneA, 1, new SaveClothingSizeRequest("m", null));
        Assert.Equal(start + 2, w.Uow.Saves);

        var after = await service.ListForDeviceAsync(PhoneA);
        Assert.Equal(("M", null), (after[0].TopSize, after[0].BottomSize));
        Assert.Equal(("12", "10"), (after[1].TopSize, after[1].BottomSize));
        Assert.Null(after[2].TopSize);
    }

    [Fact]
    public async Task A_device_cannot_touch_the_sizes_of_other_players_or_send_invalid_ones()
    {
        var (w, service) = await Linked(2);
        var start = w.Uow.Saves;

        await Assert.ThrowsAsync<DomainException>(() => service.SaveForDeviceAsync(PhoneA, 4, new SaveClothingSizeRequest("M", "M")));
        await Assert.ThrowsAsync<DomainException>(() => service.SaveForDeviceAsync(PhoneA, 2, new SaveClothingSizeRequest("M", "enorme")));
        await Assert.ThrowsAsync<UnauthorizedAppException>(() => service.SaveForDeviceAsync("phoneBBBB2", 1, new SaveClothingSizeRequest("M", "M")));
        await Assert.ThrowsAsync<UnauthorizedAppException>(() => service.ListForDeviceAsync(null));
        Assert.Equal(start, w.Uow.Saves);
        Assert.Null(w.Players.Items.Single(p => p.Id == 4).TopSize);
    }

    [Fact]
    public async Task The_administrator_sees_everybody_except_the_reserve_player_in_name_order()
    {
        var (w, service) = await Linked();
        w.Players.Items.Add(Player.CreateReserve().WithId(9));
        await service.SaveForDeviceAsync(PhoneA, 1, new SaveClothingSizeRequest("L", "XL"));

        var all = await service.ListAllAsync();

        Assert.Equal(5, all.Count);
        Assert.DoesNotContain(all, r => r.PlayerId == 9);
        Assert.Equal(all.Select(r => r.Name).OrderBy(n => n), all.Select(r => r.Name));
        Assert.Equal(("L", "XL"), (all.Single(r => r.PlayerId == 1).TopSize, all.Single(r => r.PlayerId == 1).BottomSize));
    }

    [Fact]
    public async Task Only_a_player_the_admin_authorized_sees_everybody_and_only_the_owner_counts()
    {
        var (w, service) = await Linked(2);
        await service.SaveForDeviceAsync(PhoneA, 1, new SaveClothingSizeRequest("M", "L"));

        await Assert.ThrowsAsync<ForbiddenAppException>(() => service.ListAllForDeviceAsync(PhoneA));   // sin permiso
        await Assert.ThrowsAsync<UnauthorizedAppException>(() => service.ListAllForDeviceAsync(null));  // sin vincular

        await service.SetViewerAsync(2, true);                                                          // el apadrinado tiene permiso, el titular no
        await Assert.ThrowsAsync<ForbiddenAppException>(() => service.ListAllForDeviceAsync(PhoneA));

        await service.SetViewerAsync(1, true);
        var all = await service.ListAllForDeviceAsync(PhoneA);
        Assert.Equal(5, all.Count);
        Assert.Equal("M", all.Single(r => r.PlayerId == 1).TopSize);
        Assert.True((await w.Links().GetMeAsync(PhoneA))!.CanViewSizes);
    }

    [Fact]
    public async Task The_admin_grants_and_revokes_access_and_lists_who_has_it()
    {
        var (w, service) = await Linked();
        var start = w.Uow.Saves;

        Assert.Empty(await service.ListViewersAsync());
        var viewers = await service.SetViewerAsync(3, true);
        Assert.Equal(new[] { 3 }, viewers.Select(v => v.Id));
        await service.SetViewerAsync(3, true);                                  // repetir no guarda de nuevo
        Assert.Equal(start + 1, w.Uow.Saves);

        Assert.False((await w.Links().GetMeAsync(PhoneA))!.CanViewSizes);
        Assert.Empty(await service.SetViewerAsync(3, false));
        Assert.Equal(start + 2, w.Uow.Saves);

        await Assert.ThrowsAsync<NotFoundException>(() => service.SetViewerAsync(99, true));
        w.Players.Items.Add(Player.CreateReserve().WithId(9));
        await Assert.ThrowsAsync<DomainException>(() => service.SetViewerAsync(9, true));
    }
}
