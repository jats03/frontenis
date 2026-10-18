using Frontenis.Api.Auditing;
using Frontenis.Api.Authorization;
using Frontenis.Api.Auditing.Interfaces;
using Frontenis.Api.RateLimiting;
using Frontenis.Application.Dtos;
using Frontenis.Application.Interfaces.Services;
using Frontenis.Application.Models;
using Frontenis.Application.Services;
using Frontenis.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Frontenis.Api.Controllers;

/// <summary>Registro de actividad: aviso de pantallas abiertas (al log del servidor) y consulta para el administrador.</summary>
[ApiController]
[Route("api/audit")]
public sealed class AuditController : ControllerBase
{
    private readonly IActorResolver _actors;
    private readonly IAuditQueries _queries;
    private readonly ILogger<AuditController> _logger;

    public AuditController(IActorResolver actors, IAuditQueries queries, ILogger<AuditController> logger)
    {
        _actors = actors;
        _queries = queries;
        _logger = logger;
    }

    /// <summary>Acciones registradas (POST, PUT, DELETE) con filtros por autor, texto, método y fechas (solo administrador).</summary>
    [HttpGet, AdminOnly]
    public async Task<ActionResult<AuditPageDto>> Search(
        [FromQuery] ActorKind? actor, [FromQuery] string? text, [FromQuery] string? method,
        [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to,
        [FromQuery] int page = 1, [FromQuery] int pageSize = AuditQueries.DefaultPageSize, CancellationToken ct = default) =>
        Ok(await _queries.SearchAsync(new AuditFilter(actor, text, method, from?.UtcDateTime, to?.UtcDateTime, page, pageSize), ct));

    [HttpPost("page"), EnableRateLimiting(RateLimitPolicies.PublicWrite)]
    public async Task<IActionResult> Page(PageViewRequest request, CancellationToken ct)
    {
        if (!AuditText.IsValidPage(request.Path)) return BadRequest();
        var actor = await _actors.ResolveAsync(HttpContext, ct);
        _logger.LogInformation("AUDIT {Kind} {Actor} VISTA {Path}", actor.Kind, actor.Name, AuditText.Clean(request.Path, 200));
        return NoContent();
    }
}
