using Frontenis.Domain.Entities;

namespace Frontenis.Domain.Models;

// ---------- Emparejamiento de jugadores ----------
public sealed record PairingResult(IReadOnlyList<(Player First, Player Second)> Pairs, Player? Unpaired);
