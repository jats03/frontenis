using Frontenis.Api.Authorization;
using Frontenis.Api.RateLimiting;
using Frontenis.Application.Dtos;
using Frontenis.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Frontenis.Api.Controllers;

/// <summary>Resultados de partidos de competición enviados por jugadores identificados y aprobados por el administrador.</summary>
[ApiController]
public sealed class MatchSubmissionsController : ControllerBase
{
    private readonly IMatchSubmissionService _submissions;
    private readonly IDeviceAccessResolver _devices;

    public MatchSubmissionsController(IMatchSubmissionService submissions, IDeviceAccessResolver devices)
    {
        _submissions = submissions;
        _devices = devices;
    }

    /// <summary>Envía (o corrige) el resultado: solo jugadores con el dispositivo vinculado. Queda pendiente de aprobación.</summary>
    [HttpPut("api/matches/{matchId:int}/submission"), EnableRateLimiting(RateLimitPolicies.PublicWrite)]
    public async Task<ActionResult<MatchSubmissionDto>> Submit(
        int matchId, SubmitResultRequest request, [FromHeader(Name = DeviceController.TokenHeader)] string? deviceToken, CancellationToken ct)
    {
        var access = await _devices.ResolveAsync(deviceToken, ct); // 401 si el dispositivo no está vinculado
        return Ok(await _submissions.SubmitAsync(matchId, access.OwnerPlayerId, request, ct));
    }

    /// <summary>Resultados pendientes de aprobación de una competición.</summary>
    [HttpGet("api/competitions/{id:int}/pending-results")]
    public async Task<ActionResult<IReadOnlyList<MatchSubmissionDto>>> Pending(int id, CancellationToken ct) =>
        Ok(await _submissions.ListAsync(id, ct));

    /// <summary>Aprueba el envío y lo convierte en resultado oficial (administrador).</summary>
    [HttpPost("api/match-submissions/{id:int}/approve"), AdminOnly]
    public async Task<ActionResult<MatchDto>> Approve(int id, CancellationToken ct) =>
        Ok(await _submissions.ApproveAsync(id, ct));

    /// <summary>Rechaza el envío (administrador).</summary>
    [HttpDelete("api/match-submissions/{id:int}"), AdminOnly]
    public async Task<IActionResult> Reject(int id, CancellationToken ct)
    {
        await _submissions.RejectAsync(id, ct);
        return NoContent();
    }
}
