using Frontenis.Application.Dtos;
using Frontenis.Application.Services;
using Frontenis.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Frontenis.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly ILogger<AuthController> _logger;
    public AuthController(IAuthService auth, ILogger<AuthController> logger) { _auth = auth; _logger = logger; }

    private string ClientIp => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "desconocida";

    [HttpPost("login"), EnableRateLimiting("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        try { return Ok(await _auth.LoginAsync(request, ClientIp, ct)); }
        catch (Exception ex) when (ex is UnauthorizedAppException or TooManyAttemptsException)
        {
            // Se registra el intento (sin la contraseña) para poder detectar ataques en los logs de Render.
            _logger.LogWarning("Inicio de sesión rechazado ({Reason}) desde {Ip}.", ex.GetType().Name, ClientIp);
            throw;
        }
    }

    /// <summary>Cambio de contraseña del administrador con sesión iniciada.</summary>
    [HttpPost("change-password"), Authorize(Roles = Roles.Admin), EnableRateLimiting("login")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken ct)
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!int.TryParse(raw, out var userId)) return Unauthorized();
        await _auth.ChangePasswordAsync(userId, request, ClientIp, ct);
        _logger.LogInformation("Contraseña de administrador cambiada (usuario {UserId}).", userId);
        return NoContent();
    }
}

[ApiController]
[Route("api/players")]
public sealed class PlayersController : ControllerBase
{
    private readonly IPlayerService _players;
    private readonly IPairingService _pairing;
    private readonly IPlayerHistoryService _history;

    public PlayersController(IPlayerService players, IPairingService pairing, IPlayerHistoryService history)
    {
        _players = players;
        _pairing = pairing;
        _history = history;
    }

    /// <summary>Histórico de todos los jugadores (puntos, victorias, derrotas…), opcionalmente de una competición.</summary>
    [HttpGet("history")]
    public async Task<ActionResult<IReadOnlyList<PlayerHistoryRowDto>>> History([FromQuery] int[]? competitionIds, [FromQuery] int[]? trainingIds, CancellationToken ct) =>
        Ok(await _history.RankingAsync(Filter(competitionIds, trainingIds), ct));

    private static HistoryFilter Filter(int[]? competitionIds, int[]? trainingIds) =>
        new((competitionIds ?? Array.Empty<int>()).Distinct().Take(500).ToList(),
            (trainingIds ?? Array.Empty<int>()).Distinct().Take(500).ToList());

    /// <summary>Ficha histórica de un jugador: compañeros, rivales y últimos partidos.</summary>
    [HttpGet("{id:int}/history")]
    public async Task<ActionResult<PlayerHistoryDto>> PlayerHistory(int id, [FromQuery] int[]? competitionIds, [FromQuery] int[]? trainingIds, CancellationToken ct) =>
        Ok(await _history.PlayerAsync(id, Filter(competitionIds, trainingIds), ct));

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

    [HttpPut("{id:int}/name"), Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<CompetitionSummaryDto>> Rename(int id, RenameCompetitionRequest request, CancellationToken ct) =>
        Ok(await _competitions.RenameAsync(id, request, ct));

    [HttpPut("{id:int}/sets-per-match"), Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<CompetitionSummaryDto>> ChangeSets(int id, SetsPerMatchRequest request, CancellationToken ct) =>
        Ok(await _competitions.ChangeSetsPerMatchAsync(id, request, ct));

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

    /// <summary>Forma los grupos a mano y genera el calendario.</summary>
    [HttpPost("{id:int}/draw-manual"), Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<CompetitionDetailDto>> DrawManual(int id, ManualDrawRequest request, CancellationToken ct) =>
        Ok(await _competitions.DrawManualAsync(id, request, ct));

    /// <summary>Liga rotatoria con los grupos de jugadores formados a mano.</summary>
    [HttpPost("{id:int}/draw-rotating-manual"), Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<CompetitionDetailDto>> DrawRotatingManual(int id, ManualDrawRequest request, CancellationToken ct) =>
        Ok(await _competitions.DrawRotatingManualAsync(id, request, ct));

    /// <summary>Liga rotatoria: reparte los jugadores elegidos en grupos y genera las jornadas.</summary>
    [HttpPost("{id:int}/draw-rotating"), Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<CompetitionDetailDto>> DrawRotating(int id, PlayersSelection selection, CancellationToken ct) =>
        Ok(await _competitions.DrawRotatingAsync(id, selection, ct));

    /// <summary>Liga rotatoria terminada: crea la siguiente con ascensos y descensos.</summary>
    [HttpPost("{id:int}/next-league"), Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<CompetitionSummaryDto>> NextLeague(int id, CancellationToken ct) =>
        Ok(await _competitions.CreateNextLeagueAsync(id, ct));

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

[ApiController]
[Route("api/trainings")]
public sealed class TrainingsController : ControllerBase
{
    private readonly ITrainingService _trainings;
    public TrainingsController(ITrainingService trainings) { _trainings = trainings; }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TrainingSummaryDto>>> List(CancellationToken ct) =>
        Ok(await _trainings.ListAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TrainingDetailDto>> Get(int id, CancellationToken ct) =>
        Ok(await _trainings.GetAsync(id, ct));

    [HttpPost, Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<TrainingSummaryDto>> Create(CreateTrainingRequest request, CancellationToken ct) =>
        Ok(await _trainings.CreateAsync(request, ct));

    [HttpDelete("{id:int}"), Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _trainings.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpPut("{id:int}/name"), Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<TrainingSummaryDto>> Rename(int id, RenameTrainingRequest request, CancellationToken ct) =>
        Ok(await _trainings.RenameAsync(id, request, ct));

    /// <summary>Abierto a cualquier persona (sin iniciar sesión): registrar un partido de la sesión. Borrar sigue siendo solo del administrador.</summary>
    [HttpPost("{id:int}/matches"), EnableRateLimiting("public-write")]
    public async Task<ActionResult<TrainingDetailDto>> AddMatch(int id, AddTrainingMatchRequest request, CancellationToken ct) =>
        Ok(await _trainings.AddMatchAsync(id, request, ct));

    [HttpDelete("{id:int}/matches/{matchId:int}"), Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<TrainingDetailDto>> RemoveMatch(int id, int matchId, CancellationToken ct) =>
        Ok(await _trainings.RemoveMatchAsync(id, matchId, ct));

    /// <summary>Solo lectura (no guarda nada): propone los próximos partidos entre los jugadores presentes.</summary>
    [HttpPost("{id:int}/suggestions")]
    public async Task<ActionResult<IReadOnlyList<TrainingSuggestionDto>>> Suggest(int id, TrainingSuggestRequest request, CancellationToken ct) =>
        Ok(await _trainings.SuggestAsync(id, request, ct));
}
