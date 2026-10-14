namespace Frontenis.Domain.Models;

// ---------- Cuadro de eliminatorias ----------
public sealed record BracketNode(int Round, int Position, int? NextRound, int? NextPosition, Frontenis.Domain.Enums.BracketSlot? NextSlot);
