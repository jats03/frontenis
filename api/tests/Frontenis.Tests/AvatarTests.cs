using Frontenis.Application.Dtos;
using Frontenis.Application.Exceptions;
using Frontenis.Application.Services;
using Frontenis.Domain.Entities;
using Frontenis.Domain.Enums;
using Frontenis.Domain.Exceptions;
using Xunit;

namespace Frontenis.Tests;

/// <summary>Avatar elegible: lo cambia el titular del dispositivo o un apadrinado; varias personas pueden tener el mismo.</summary>
public class AvatarTests
{
    private const string PhoneA = "phoneAAAA1";

    [Theory]
    [InlineData(1)]
    [InlineData(18)]
    [InlineData(Player.MaxAvatar)]
    public void A_valid_avatar_is_saved(int seed)
    {
        var p = new Player("Ana", PlayerPosition.Zaguero);
        p.SetAvatar(seed);
        Assert.Equal(seed, p.AvatarSeed);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(Player.MaxAvatar + 1)]
    public void An_avatar_out_of_range_is_rejected_and_nothing_changes(int seed)
    {
        var p = new Player("Ana", PlayerPosition.Zaguero);
        p.SetAvatar(5);
        Assert.Throws<DomainException>(() => p.SetAvatar(seed));
        Assert.Equal(5, p.AvatarSeed);
    }

    [Fact]
    public async Task The_player_list_carries_the_avatar_and_an_empty_one_goes_back_to_the_original()
    {
        var players = new FakePlayers();
        var p = new Player("Ana", PlayerPosition.Zaguero).WithId(3);
        players.Items.Add(p);
        var queries = new PlayerQueries(players);

        Assert.Null((await queries.ListAsync()).Single().AvatarSeed);   // sin elegir: el de siempre

        p.SetAvatar(12);
        Assert.Equal(12, (await queries.ListAsync()).Single().AvatarSeed);

        p.SetAvatar(null);
        Assert.Null((await queries.ListAsync()).Single().AvatarSeed);
    }

    private static async Task<(BookingWorld World, AvatarService Service)> Linked(params int[] delegateIds)
    {
        var w = new BookingWorld();
        await w.Links().LinkAsync(PhoneA, 1);
        foreach (var d in delegateIds) await w.DelegateService().AddAsync(new AddPlayerDelegateRequest(1, d));
        return (w, new AvatarService(w.Access(), w.Players, w.Uow));
    }

    [Fact]
    public async Task The_owner_and_a_delegate_can_change_their_avatar_and_several_players_can_share_one()
    {
        var (w, service) = await Linked(2);
        var saves = w.Uow.Saves;

        var mine = await service.SetForDeviceAsync(PhoneA, 1, 7);
        var theirs = await service.SetForDeviceAsync(PhoneA, 2, 7);   // el mismo avatar: permitido

        Assert.Equal((1, 7), (mine.PlayerId, mine.AvatarSeed));
        Assert.Equal((2, 7), (theirs.PlayerId, theirs.AvatarSeed));
        Assert.Equal(7, w.Players.Items.Single(p => p.Id == 2).AvatarSeed);
        Assert.Equal(saves + 2, w.Uow.Saves);

        var back = await service.SetForDeviceAsync(PhoneA, 1, null);
        Assert.Null(back.AvatarSeed);
    }

    [Fact]
    public async Task A_device_cannot_change_the_avatar_of_other_players_or_use_an_invalid_one_and_nothing_is_saved()
    {
        var (w, service) = await Linked(2);
        var saves = w.Uow.Saves;

        await Assert.ThrowsAsync<DomainException>(() => service.SetForDeviceAsync(PhoneA, 4, 3));                 // ni titular ni apadrinado
        await Assert.ThrowsAsync<DomainException>(() => service.SetForDeviceAsync(PhoneA, 1, Player.MaxAvatar + 1)); // avatar no válido
        await Assert.ThrowsAsync<UnauthorizedAppException>(() => service.SetForDeviceAsync("phoneBBBB2", 1, 3));  // sin vincular
        await Assert.ThrowsAsync<UnauthorizedAppException>(() => service.SetForDeviceAsync(null, 1, 3));

        Assert.Equal(saves, w.Uow.Saves);
        Assert.All(w.Players.Items, p => Assert.Null(p.AvatarSeed));
    }
}
