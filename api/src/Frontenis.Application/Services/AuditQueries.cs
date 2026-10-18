using Frontenis.Application.Dtos;
using Frontenis.Application.Interfaces.Repositories;
using Frontenis.Application.Interfaces.Services;
using Frontenis.Application.Models;

namespace Frontenis.Application.Services;

/// <summary>Implementación de <see cref="IAuditQueries"/>.</summary>
public sealed class AuditQueries : IAuditQueries
{
    public const int MaxPageSize = 100;
    public const int DefaultPageSize = 50;

    private readonly IAuditRepository _audit;
    public AuditQueries(IAuditRepository audit) { _audit = audit; }

    public async Task<AuditPageDto> SearchAsync(AuditFilter filter, CancellationToken ct = default)
    {
        var size = filter.PageSize <= 0 ? DefaultPageSize : Math.Min(filter.PageSize, MaxPageSize);
        var page = Math.Max(1, filter.Page);
        var method = string.IsNullOrWhiteSpace(filter.Method) ? null : filter.Method.Trim().ToUpperInvariant();
        var text = string.IsNullOrWhiteSpace(filter.Text) ? null : filter.Text.Trim();
        var (items, total) = await _audit.SearchAsync(filter with { Page = page, PageSize = size, Method = method, Text = text }, ct);
        return new AuditPageDto(
            items.Select(a => new AuditEntryDto(a.Id, a.AtUtc, a.Actor, a.ActorName, a.PlayerId, a.Method, a.Path, a.Status, a.DurationMs)).ToList(),
            total, page, size);
    }
}
