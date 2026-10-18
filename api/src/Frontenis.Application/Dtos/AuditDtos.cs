using Frontenis.Domain.Enums;

namespace Frontenis.Application.Dtos;

// ---------- Registro de actividad ----------
public sealed record AuditEntryDto(
    int Id, DateTime AtUtc, ActorKind Actor, string ActorName, int? PlayerId,
    string Method, string Path, int Status, int DurationMs);

public sealed record AuditPageDto(IReadOnlyList<AuditEntryDto> Items, int Total, int Page, int PageSize);
