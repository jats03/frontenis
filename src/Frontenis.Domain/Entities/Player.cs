using Frontenis.Domain.Enums;
using Frontenis.Domain.Exceptions;

namespace Frontenis.Domain.Entities;

public sealed class Player
{
    private Player() { Name = string.Empty; }

    public Player(string name, PlayerPosition position)
    {
        Name = Validate(name);
        Position = position;
    }

    public int Id { get; private set; }
    public string Name { get; private set; }
    public PlayerPosition Position { get; private set; }

    public void Update(string name, PlayerPosition position)
    {
        Name = Validate(name);
        Position = position;
    }

    private static string Validate(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("El nombre del jugador es obligatorio.");
        return name.Trim();
    }
}
