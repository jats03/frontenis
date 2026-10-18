using Frontenis.Domain.Enums;

namespace Frontenis.Application.Models;

/// <summary>Filtros y página que pide el administrador al consultar el registro de actividad.</summary>
public sealed record AuditFilter(
    ActorKind? Actor, string? Text, string? Method, DateTime? FromUtc, DateTime? ToUtc, int Page, int PageSize);
