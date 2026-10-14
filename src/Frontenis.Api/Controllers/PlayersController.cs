using Frontenis.Api.Authorization;
using Frontenis.Application.Dtos;
using Frontenis.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace Frontenis.Api.Controllers;

[ApiController]
[Route("api/players")]
public sealed class PlayersController : ControllerBase
{
    private readonly IPlayerQueries _queries;
    private readonly IPlayerCommands _commands;
    private readonly IPairingService _pairing;

    public PlayersController(IPlayerQueries queries, IPlayerCommands commands, IPairingService pairing)
    {
        _queries = queries;
        _commands = commands;
        _pairing = pairing;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PlayerDto>>> List(CancellationToken ct) =>
        Ok(await _queries.ListAsync(ct));

    [HttpPost, AdminOnly]
    public async Task<ActionResult<PlayerDto>> Create(PlayerRequest request, CancellationToken ct) =>
        Ok(await _commands.CreateAsync(request, ct));

    [HttpPut("{id:int}"), AdminOnly]
    public async Task<ActionResult<PlayerDto>> Update(int id, PlayerRequest request, CancellationToken ct) =>
        Ok(await _commands.UpdateAsync(id, request, ct));

    [HttpDelete("{id:int}"), AdminOnly]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _commands.DeleteAsync(id, ct);
        return NoContent();
    }

    /// <summary>Vista previa de un emparejamiento aleatorio (zaguero + delantero).</summary>
    [HttpPost("pairing-preview"), AdminOnly]
    public async Task<ActionResult<PairingPreviewDto>> Preview(PlayersSelection selection, CancellationToken ct) =>
        Ok(await _pairing.PreviewAsync(selection, ct));
}
