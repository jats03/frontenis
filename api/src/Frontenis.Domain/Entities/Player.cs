using Frontenis.Domain.Enums;
using Frontenis.Domain.Exceptions;

namespace Frontenis.Domain.Entities;

/// <summary>Jugador del club, con su posición (zaguero o delantero) y un grupo/nivel opcional.</summary>
public sealed class Player
{
    private Player() { Name = string.Empty; }

    public Player(string name, PlayerPosition position, int? groupNumber = null)
    {
        Name = Validate(name);
        Position = position;
        GroupNumber = ValidateGroup(groupNumber);
    }

    public int Id { get; private set; }
    public string Name { get; private set; }
    public PlayerPosition Position { get; private set; }
    /// <summary>Grupo de jugadores al que pertenece (1, 2, 3…) para seleccionarlos juntos; null = sin grupo.</summary>
    public int? GroupNumber { get; private set; }
    /// <summary>Jugador comodín «Jugador reserva»: cubre la ausencia de un jugador en un partido. No es un jugador real, no puntúa ni aparece en listas.</summary>
    public bool IsReserve { get; private set; }

    /// <summary>Talla de la parte de arriba (camiseta); null = aún sin indicar.</summary>
    public string? TopSize { get; private set; }
    /// <summary>Talla de la parte de abajo (pantalón); null = aún sin indicar.</summary>
    public string? BottomSize { get; private set; }

    /// <summary>Guarda las tallas de ropa (vacío = sin indicar).</summary>
    /// <exception cref="DomainException">Alguna talla no es válida.</exception>
    public void SetClothingSizes(string? top, string? bottom)
    {
        var t = Services.ClothingSizes.Normalize(top);
        var b = Services.ClothingSizes.Normalize(bottom);
        TopSize = t;
        BottomSize = b;
    }

    /// <summary>Nombre con el que se muestra al jugador reserva.</summary>
    public const string ReserveName = "Jugador reserva";

    /// <summary>Crea el jugador comodín que ocupa el hueco de un jugador ausente.</summary>
    public static Player CreateReserve() => new(ReserveName, PlayerPosition.Delantero) { IsReserve = true };

    /// <summary>Modifica nombre, posición y número de grupo (1-99, opcional).</summary>
    public void Update(string name, PlayerPosition position, int? groupNumber = null)
    {
        // Un nombre antiguo que ya no cumple el límite de longitud se puede conservar (al cambiar posición o grupo).
        var clean = Services.PlayerNameRules.Normalize(name);
        Name = clean == Name ? Name : Validate(name);
        Position = position;
        GroupNumber = ValidateGroup(groupNumber);
    }

    private static int? ValidateGroup(int? g)
    {
        if (g is < 1 or > 99) throw new DomainException("El grupo debe estar entre 1 y 99.");
        return g;
    }

    private static string Validate(string name) => Services.PlayerNameRules.Validate(name);
}
