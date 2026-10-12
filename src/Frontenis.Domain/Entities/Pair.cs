using Frontenis.Domain.Exceptions;

namespace Frontenis.Domain.Entities;

/// <summary>Pareja de jugadores (un zaguero y un delantero salvo caso extremo).</summary>
public sealed class Pair
{
    private Pair() { Player1 = null!; Player2 = null!; }

    public Pair(Player player1, Player player2)
    {
        if (player1 is null || player2 is null) throw new DomainException("Una pareja necesita dos jugadores.");
        if (ReferenceEquals(player1, player2) || (player1.Id != 0 && player1.Id == player2.Id))
            throw new DomainException("Una pareja no puede repetir el mismo jugador.");
        Player1 = player1;
        Player1Id = player1.Id;
        Player2 = player2;
        Player2Id = player2.Id;
    }

    public int Id { get; private set; }
    public int Player1Id { get; private set; }
    public Player Player1 { get; private set; }
    public int Player2Id { get; private set; }
    public Player Player2 { get; private set; }

    public string DisplayName => $"{Player1.Name} / {Player2.Name}";

    public bool Contains(int playerId) => Player1Id == playerId || Player2Id == playerId;
}
