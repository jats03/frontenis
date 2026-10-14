namespace Frontenis.Domain.Models;

public sealed record BracketPlacement(int Round, int Position, Frontenis.Domain.Enums.BracketSlot Slot, int Seed);
