using Frontenis.Application.Dtos;
using Frontenis.Application.Services;
using Frontenis.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frontenis.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    public AuthController(IAuthService auth) { _auth = auth; }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken ct) =>
        Ok(await _auth.LoginAsync(request, ct));
}

[ApiController]
[Route("api/players")]
public sealed class PlayersController : ControllerBase
{
    private readonly IPlayerService _players;
    private readonly IPairingService _pairing;

    public PlayersController(IPlayerService players, IPairingService pairing)
    {
        _players = players;
        _pairing = pairing;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PlayerDto>>> List(CancellationToken ct) =>
        Ok(await _players.ListAsync(ct));

    [HttpPost, Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<PlayerDto>> Create(PlayerRequest request, CancellationToken ct) =>
        Ok(await _players.CreateAsync(request, ct));

    [HttpPut("{id:int}"), Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<PlayerDto>> Update(int id, PlayerRequest request, CancellationToken ct) =>
        Ok(await _players.UpdateAsync(id, request, ct));

    [HttpDelete("{id:int}"), Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _players.DeleteAsync(id, ct);
        return NoContent();
    }

    /// <summary>Vista previa de un emparejamiento aleatorio (zaguero + delantero).</summary>
    [HttpPost("pairing-preview"), Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<PairingPreviewDto>> Preview(PlayersSelection selection, CancellationToken ct) =>
        Ok(await _pairing.PreviewAsync(selection, ct));
}

[ApiController]
[Route("api/competitions")]
public sealed class CompetitionsController : ControllerBase
{
    private readonly ICompetitionService _competitions;
    private readonly IPairingService _pairing;

    public CompetitionsController(ICompetitionService competitions, IPairingService pairing)
    {
        _competitions = competitions;
        _pairing = pairing;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CompetitionSummaryDto>>> List(CancellationToken ct) =>
        Ok(await _competitions.ListAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CompetitionDetailDto>> Get(int id, CancellationToken ct) =>
        Ok(await _competitions.GetAsync(id, ct));

    [HttpGet("{id:int}/standings")]
    public async Task<ActionResult<IReadOnlyList<GroupStandingsDto>>> Standings(int id, CancellationToken ct) =>
        Ok(await _competitions.GetStandingsAsync(id, ct));

    [HttpPost, Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<CompetitionSummaryDto>> Create(CreateCompetitionRequest request, CancellationToken ct) =>
        Ok(await _competitions.CreateAsync(request, ct));

    [HttpDelete("{id:int}"), Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _competitions.DeleteAsync(id, ct);
        return NoContent();
    }

    /// <summary>Inscribe parejas generadas aleatoriamente a partir de los jugadores indicados.</summary>
    [HttpPost("{id:int}/pairs/random"), Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<IReadOnlyList<PairDto>>> RegisterRandom(int id, PlayersSelection selection, CancellationToken ct) =>
        Ok(await _pairing.RegisterRandomAsync(id, selection, ct));

    [HttpPost("{id:int}/pairs"), Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<PairDto>> RegisterManual(int id, ManualPairRequest request, CancellationToken ct) =>
        Ok(await _pairing.RegisterManualAsync(id, request, ct));

    /// <summary>Sortea los grupos y genera el calendario.</summary>
    [HttpPost("{id:int}/draw"), Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<CompetitionDetailDto>> Draw(int id, CancellationToken ct) =>
        Ok(await _competitions.DrawAsync(id, ct));

    [HttpPost("{id:int}/playoffs"), Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<CompetitionDetailDto>> Playoffs(int id, CancellationToken ct) =>
        Ok(await _competitions.GeneratePlayoffsAsync(id, ct));
}

[ApiController]
[Route("api/matches")]
public sealed class MatchesController : ControllerBase
{
    private readonly IMatchService _matches;
    public MatchesController(IMatchService matches) { _matches = matches; }

    [HttpPut("{id:int}/result"), Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<MatchDto>> SetResult(int id, SetResultRequest request, CancellationToken ct) =>
        Ok(await _matches.SetResultAsync(id, request, ct));

    [HttpDelete("{id:int}/result"), Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<MatchDto>> ClearResult(int id, CancellationToken ct) =>
        Ok(await _matches.ClearResultAsync(id, ct));
}
