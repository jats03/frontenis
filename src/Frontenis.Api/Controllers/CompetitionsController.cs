using Frontenis.Api.Authorization;
using Frontenis.Application.Dtos;
using Frontenis.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace Frontenis.Api.Controllers;

[ApiController]
[Route("api/competitions")]
public sealed class CompetitionsController : ControllerBase
{
    private readonly ICompetitionQueries _queries;
    private readonly ICompetitionCommands _commands;
    private readonly ICompetitionDrawService _draws;
    private readonly IPairingService _pairing;

    public CompetitionsController(ICompetitionQueries queries, ICompetitionCommands commands,
        ICompetitionDrawService draws, IPairingService pairing)
    {
        _queries = queries;
        _commands = commands;
        _draws = draws;
        _pairing = pairing;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CompetitionSummaryDto>>> List(CancellationToken ct) =>
        Ok(await _queries.ListAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CompetitionDetailDto>> Get(int id, CancellationToken ct) =>
        Ok(await _queries.GetAsync(id, ct));

    [HttpGet("{id:int}/standings")]
    public async Task<ActionResult<IReadOnlyList<GroupStandingsDto>>> Standings(int id, CancellationToken ct) =>
        Ok(await _queries.GetStandingsAsync(id, ct));

    [HttpPost, AdminOnly]
    public async Task<ActionResult<CompetitionSummaryDto>> Create(CreateCompetitionRequest request, CancellationToken ct) =>
        Ok(await _commands.CreateAsync(request, ct));

    [HttpPut("{id:int}/name"), AdminOnly]
    public async Task<ActionResult<CompetitionSummaryDto>> Rename(int id, RenameCompetitionRequest request, CancellationToken ct) =>
        Ok(await _commands.RenameAsync(id, request, ct));

    [HttpPut("{id:int}/sets-per-match"), AdminOnly]
    public async Task<ActionResult<CompetitionSummaryDto>> ChangeSets(int id, SetsPerMatchRequest request, CancellationToken ct) =>
        Ok(await _commands.ChangeSetsPerMatchAsync(id, request, ct));

    [HttpDelete("{id:int}"), AdminOnly]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _commands.DeleteAsync(id, ct);
        return NoContent();
    }

    /// <summary>Inscribe parejas generadas aleatoriamente a partir de los jugadores indicados.</summary>
    [HttpPost("{id:int}/pairs/random"), AdminOnly]
    public async Task<ActionResult<IReadOnlyList<PairDto>>> RegisterRandom(int id, PlayersSelection selection, CancellationToken ct) =>
        Ok(await _pairing.RegisterRandomAsync(id, selection, ct));

    [HttpPost("{id:int}/pairs"), AdminOnly]
    public async Task<ActionResult<PairDto>> RegisterManual(int id, ManualPairRequest request, CancellationToken ct) =>
        Ok(await _pairing.RegisterManualAsync(id, request, ct));

    /// <summary>Sortea los grupos y genera el calendario.</summary>
    [HttpPost("{id:int}/draw"), AdminOnly]
    public async Task<ActionResult<CompetitionDetailDto>> Draw(int id, CancellationToken ct) =>
        Ok(await _draws.DrawAsync(id, ct));

    /// <summary>Forma los grupos a mano y genera el calendario.</summary>
    [HttpPost("{id:int}/draw-manual"), AdminOnly]
    public async Task<ActionResult<CompetitionDetailDto>> DrawManual(int id, ManualDrawRequest request, CancellationToken ct) =>
        Ok(await _draws.DrawManualAsync(id, request, ct));

    /// <summary>Liga rotatoria con los grupos de jugadores formados a mano.</summary>
    [HttpPost("{id:int}/draw-rotating-manual"), AdminOnly]
    public async Task<ActionResult<CompetitionDetailDto>> DrawRotatingManual(int id, ManualDrawRequest request, CancellationToken ct) =>
        Ok(await _draws.DrawRotatingManualAsync(id, request, ct));

    /// <summary>Liga rotatoria: reparte los jugadores elegidos en grupos y genera las jornadas.</summary>
    [HttpPost("{id:int}/draw-rotating"), AdminOnly]
    public async Task<ActionResult<CompetitionDetailDto>> DrawRotating(int id, PlayersSelection selection, CancellationToken ct) =>
        Ok(await _draws.DrawRotatingAsync(id, selection, ct));

    /// <summary>Liga rotatoria terminada: crea la siguiente con ascensos y descensos.</summary>
    [HttpPost("{id:int}/next-league"), AdminOnly]
    public async Task<ActionResult<CompetitionSummaryDto>> NextLeague(int id, CancellationToken ct) =>
        Ok(await _draws.CreateNextLeagueAsync(id, ct));

    [HttpPost("{id:int}/playoffs"), AdminOnly]
    public async Task<ActionResult<CompetitionDetailDto>> Playoffs(int id, CancellationToken ct) =>
        Ok(await _draws.GeneratePlayoffsAsync(id, ct));
}
