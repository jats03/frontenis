namespace Frontenis.Domain.Models;

/// <summary>Franja prevista por una plantilla, aún sin crear.</summary>
public sealed record PlannedSlot(int TemplateId, DateOnly Date, TimeOnly Start, TimeOnly End);
