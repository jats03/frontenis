using Frontenis.Api.Auditing;
using Frontenis.Api.Auditing.Interfaces;
using Frontenis.Api.RateLimiting;
using Frontenis.Application.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Frontenis.Api.Controllers;

/// <summary>Aviso de la web cada vez que alguien abre una pantalla (navegación). Solo va al log del servidor.</summary>
[ApiController]
[Route("api/audit")]
public sealed class AuditController : ControllerBase
{
    private readonly IActorResolver _actors;
    private readonly ILogger<AuditController> _logger;

    public AuditController(IActorResolver actors, ILogger<AuditController> logger)
    {
        _actors = actors;
        _logger = logger;
    }

    [HttpPost("page"), EnableRateLimiting(RateLimitPolicies.PublicWrite)]
    public async Task<IActionResult> Page(PageViewRequest request, CancellationToken ct)
    {
        if (!AuditText.IsValidPage(request.Path)) return BadRequest();
        var actor = await _actors.ResolveAsync(HttpContext, ct);
        _logger.LogInformation("AUDIT {Kind} {Actor} VISTA {Path}", actor.Kind, actor.Name, AuditText.Clean(request.Path, 200));
        return NoContent();
    }
}
