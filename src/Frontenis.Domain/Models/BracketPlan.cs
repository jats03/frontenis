namespace Frontenis.Domain.Models;

public sealed record BracketPlan(IReadOnlyList<BracketNode> Nodes, IReadOnlyList<BracketPlacement> Placements);
