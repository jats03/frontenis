using Frontenis.Api.Authorization;
using Frontenis.Application.Dtos;
using Frontenis.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace Frontenis.Api.Controllers;

/// <summary>Apadrinados: qué jugador puede apuntar a qué otros (solo administrador).</summary>
[ApiController, AdminOnly]
[Route("api/player-delegates")]
public sealed class PlayerDelegatesController : ControllerBase
{
    private readonly IPlayerDelegateQueries _queries;
    private readonly IPlayerDelegateCommands _commands;

    public PlayerDelegatesController(IPlayerDelegateQueries queries, IPlayerDelegateCommands commands)
    {
        _queries = queries;
        _commands = commands;
    }

    /// <summary>Lista quién puede apuntar a quién.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PlayerDelegateDto>>> List(CancellationToken ct) =>
        Ok(await _queries.ListAsync(ct));

    /// <summary>Permite que un jugador apunte también a otro.</summary>
    [HttpPost]
    public async Task<ActionResult<PlayerDelegateDto>> Add(AddPlayerDelegateRequest request, CancellationToken ct) =>
        Ok(await _commands.AddAsync(request, ct));

    /// <summary>Quita el apadrinamiento.</summary>
    [HttpDelete("{ownerId:int}/{delegateId:int}")]
    public async Task<IActionResult> Remove(int ownerId, int delegateId, CancellationToken ct)
    {
        await _commands.RemoveAsync(ownerId, delegateId, ct);
        return NoContent();
    }
}
