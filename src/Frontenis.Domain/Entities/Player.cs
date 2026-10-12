using Frontenis.Domain.Enums;
using Frontenis.Domain.Exceptions;

namespace Frontenis.Domain.Entities;

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

    public void Update(string name, PlayerPosition position, int? groupNumber = null)
    {
        Name = Validate(name);
        Position = position;
        GroupNumber = ValidateGroup(groupNumber);
    }

    private static int? ValidateGroup(int? g)
    {
        if (g is < 1 or > 99) throw new DomainException("El grupo debe estar entre 1 y 99.");
        return g;
    }

    private static string Validate(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("El nombre del jugador es obligatorio.");
        return name.Trim();
    }
}
