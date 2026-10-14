using Frontenis.Api.Authorization;
using Frontenis.Application.Dtos;
using Frontenis.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace Frontenis.Api.Controllers;

[ApiController]
[Route("api/matches")]
public sealed class MatchesController : ControllerBase
{
    private readonly IMatchService _matches;
    public MatchesController(IMatchService matches) { _matches = matches; }

    /// <summary>Registra o corrige el resultado de un partido (administrador).</summary>
    [HttpPut("{id:int}/result"), AdminOnly]
    public async Task<ActionResult<MatchDto>> SetResult(int id, SetResultRequest request, CancellationToken ct) =>
        Ok(await _matches.SetResultAsync(id, request, ct));

    /// <summary>Borra el resultado de un partido (administrador).</summary>
    [HttpDelete("{id:int}/result"), AdminOnly]
    public async Task<ActionResult<MatchDto>> ClearResult(int id, CancellationToken ct) =>
        Ok(await _matches.ClearResultAsync(id, ct));
}
