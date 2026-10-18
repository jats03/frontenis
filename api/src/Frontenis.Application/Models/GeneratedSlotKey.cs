namespace Frontenis.Application.Models;

/// <summary>Identifica la franja que una plantilla ya generó en una semana (el lunes de esa semana).</summary>
public sealed record GeneratedSlotKey(int TemplateId, DateOnly WeekStart);
