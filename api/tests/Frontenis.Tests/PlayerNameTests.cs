using Frontenis.Application.Dtos;
using Frontenis.Application.Services;
using Frontenis.Domain.Entities;
using Frontenis.Domain.Enums;
using Frontenis.Domain.Exceptions;
using Frontenis.Domain.Services;
using Xunit;

namespace Frontenis.Tests;

public class PlayerNameTests
{
    [Theory]
    [InlineData("  José   Alejandro  ", "José Alejandro")]
    [InlineData("José\tAlejandro", "José Alejandro")]
    [InlineData("José\u00A0Alejandro", "José Alejandro")]            // espacio duro
    [InlineData("José\u2003Alejandro", "José Alejandro")]            // espacio ancho
    [InlineData("Jo\u200Bsé Alejandro", "José Alejandro")]           // espacio de ancho cero (invisible)
    [InlineData("\u202EJosé\u202C Alejandro", "José Alejandro")]    // marcas de dirección
    [InlineData("José\r\nAlejandro", "José Alejandro")]
    [InlineData("Jose\u0301 Alejandro", "José Alejandro")]            // e + tilde combinada -> é
    public void Names_are_cleaned(string input, string expected) => Assert.Equal(expected, PlayerNameRules.Normalize(input));

    [Theory]
    [InlineData("José Alejandro", "jose alejandro")]
    [InlineData("José  Alejandro", "JOSE ALEJANDRO")]
    [InlineData("José Alejandro", "Jose-Alejandro")]
    [InlineData("José Alejandro", " jose   alejandro ")]
    [InlineData("José Alejandro", "José\u00A0Alejandro")]
    [InlineData("José Alejandro", "Jo\u200Bse Alejandro")]
    [InlineData("Mª Ángeles", "ma angeles")]
    [InlineData("O\'Brian", "OBrian")]
    [InlineData("Muñoz", "MUNOZ")]
    [InlineData("Straße", "strasse")]
    public void Equivalent_names_share_the_same_key(string a, string b)
    {
        Assert.Equal(PlayerNameRules.Key(a), PlayerNameRules.Key(b));
        Assert.True(PlayerNameRules.Same(a, b));
    }

    [Theory]
    [InlineData("José", "Joseph")]
    [InlineData("Ana María", "Ana Marta")]
    [InlineData("Luis", "Luis 2")]
    public void Different_names_have_different_keys(string a, string b) => Assert.False(PlayerNameRules.Same(a, b));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\u200B\u200B")]
    [InlineData("---")]
    [InlineData("😀")]
    public void Invalid_names_are_rejected(string name)
    {
        Assert.Throws<DomainException>(() => PlayerNameRules.Validate(name));
        Assert.False(PlayerNameRules.Same(name, name));
    }

    [Fact]
    public void Too_long_names_are_rejected()
    {
        Assert.Equal(40, PlayerNameRules.MaxLength);
        PlayerNameRules.Validate(new string('a', 40));
        Assert.Throws<DomainException>(() => PlayerNameRules.Validate(new string('a', 41)));
        Assert.Throws<DomainException>(() => PlayerNameRules.Validate("Maria de los Angeles Fernandez de la Torre Alvarez"));
    }

    [Fact]
    public async Task A_legacy_long_name_can_be_kept_but_not_lengthened_or_set_again()
    {
        var long41 = new string('b', 41);
        var players = new FakePlayers();
        var legacy = TestHelpers.Player(1, "x", PlayerPosition.Delantero);
        typeof(Player).GetProperty(nameof(Player.Name))!.SetValue(legacy, long41);   // simula un nombre guardado antes del límite
        players.Items.Add(legacy);
        players.Items.Add(TestHelpers.Player(2, "Ana", PlayerPosition.Delantero));
        var cmd = new PlayerCommands(players, new FakeUow());
        await cmd.UpdateAsync(1, new PlayerRequest(long41, PlayerPosition.Zaguero, 3));          // mismo nombre: solo cambia la posición
        await Assert.ThrowsAsync<DomainException>(() => cmd.UpdateAsync(1, new PlayerRequest(long41 + "c", PlayerPosition.Zaguero)));
        await Assert.ThrowsAsync<DomainException>(() => cmd.UpdateAsync(2, new PlayerRequest(long41, PlayerPosition.Delantero)));
    }

    [Fact]
    public void A_player_stores_the_clean_name() =>
        Assert.Equal("José Alejandro", new Player("  José\u00A0  Alejandro ", PlayerPosition.Delantero).Name);

    private static (PlayerCommands, FakePlayers) Setup(params string[] names)
    {
        var players = new FakePlayers();
        var id = 1;
        foreach (var n in names) players.Items.Add(TestHelpers.Player(id++, n, PlayerPosition.Delantero));
        return (new PlayerCommands(players, new FakeUow()), players);
    }

    [Fact]
    public async Task Creating_a_player_with_an_equivalent_name_is_rejected()
    {
        var (cmd, players) = Setup("José Alejandro");
        foreach (var dup in new[] { "jose alejandro", "  José   Alejandro ", "JOSÉ ALEJANDRO", "Jose-Alejandro", "José\u00A0Alejandro" })
            await Assert.ThrowsAsync<DomainException>(() => cmd.CreateAsync(new PlayerRequest(dup, PlayerPosition.Zaguero)));
        Assert.Single(players.Items);
        var ok = await cmd.CreateAsync(new PlayerRequest("  Ana   López ", PlayerPosition.Zaguero));
        Assert.Equal("Ana López", ok.Name);
    }

    [Fact]
    public async Task Renaming_checks_other_players_but_not_the_player_itself()
    {
        var (cmd, players) = Setup("José Alejandro", "Ana López");
        await Assert.ThrowsAsync<DomainException>(() => cmd.UpdateAsync(2, new PlayerRequest("jose alejandro", PlayerPosition.Delantero)));
        Assert.Equal("Ana López", players.Items[1].Name);

        // cambiar solo mayúsculas, tildes o espacios del propio nombre está permitido
        var fixedName = await cmd.UpdateAsync(2, new PlayerRequest("ANA  LÓPEZ", PlayerPosition.Delantero));
        Assert.Equal("ANA LÓPEZ", fixedName.Name);
        // mismo nombre (cambio de posición o de grupo)
        await cmd.UpdateAsync(1, new PlayerRequest("José Alejandro", PlayerPosition.Zaguero, 2));
        // un nombre libre
        var renamed = await cmd.UpdateAsync(2, new PlayerRequest("Ana M. López", PlayerPosition.Delantero));
        Assert.Equal("Ana M. López", renamed.Name);
        await Assert.ThrowsAsync<NotFoundException>(() => cmd.UpdateAsync(99, new PlayerRequest("X", PlayerPosition.Delantero)));
    }

    [Fact]
    public async Task Legacy_duplicates_can_still_change_position()
    {
        var (cmd, _) = Setup("Ana", "ana", "Luis");
        await cmd.UpdateAsync(2, new PlayerRequest("ana", PlayerPosition.Zaguero));   // mismo nombre: no se revisa
        await Assert.ThrowsAsync<DomainException>(() => cmd.UpdateAsync(3, new PlayerRequest("ANA!", PlayerPosition.Zaguero)));
    }

    [Fact]
    public async Task The_reserve_name_is_reserved()
    {
        var (cmd, _) = Setup("Ana");
        await Assert.ThrowsAsync<DomainException>(() => cmd.CreateAsync(new PlayerRequest("jugador  reserva", PlayerPosition.Delantero)));
        await Assert.ThrowsAsync<DomainException>(() => cmd.UpdateAsync(1, new PlayerRequest("Jugador Reserva", PlayerPosition.Delantero)));
    }
}
