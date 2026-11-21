using Frontenis.Application.Dtos;
using Frontenis.Application.Extensions;
using Frontenis.Application.Interfaces.Repositories;
using Frontenis.Application.Interfaces.Services;
using Frontenis.Application.Models;
using Frontenis.Domain.Interfaces;
using Frontenis.Domain.Models;

namespace Frontenis.Application.Services;

/// <summary>Calcula el histórico a partir de los partidos jugados; no se cachea porque depende del filtro.</summary>
public sealed class PlayerHistoryService : IPlayerHistoryService
{
    private const int RecentCount = 15;

    private readonly IMatchHistoryRepository _history;
    private readonly IPlayerRepository _players;
    private readonly IPlayerStatsCalculator _calculator;

    public PlayerHistoryService(IMatchHistoryRepository history, IPlayerRepository players, IPlayerStatsCalculator calculator)
    {
        _history = history;
        _players = players;
        _calculator = calculator;
    }

    public async Task<IReadOnlyList<PlayerHistoryRowDto>> RankingAsync(HistoryFilter filter, CancellationToken ct = default)
    {
        var players = await _players.ListAsync(ct);
        var stats = _calculator.CalculateAll(await _history.ListPlayedAsync(filter, null, ct))
            .ToDictionary(s => s.PlayerId);
        return players
            .Select(p => ToRow(p.Id, p.Name, p.Position, stats.GetValueOrDefault(p.Id)))
            .OrderByDescending(r => r.ClassificationPoints).ThenByDescending(r => r.Won)
            .ThenByDescending(r => r.TantosDiff).ThenBy(r => r.Name)
            .ToList();
    }

    public async Task<PlayerHistoryDto> PlayerAsync(int playerId, HistoryFilter filter, CancellationToken ct = default)
    {
        var player = await _players.GetOrThrowAsync(playerId, ct);
        var names = (await _players.ListAsync(ct)).ToDictionary(p => p.Id, p => p.Name);
        var matches = await _history.ListPlayedAsync(filter, playerId, ct);
        var stats = _calculator.Calculate(playerId, matches);

        string Name(int id) => names.GetValueOrDefault(id, "—");
        // Un lado con un solo jugador es que el otro hueco lo cubrió un comodín: el «Jugador reserva» en las competiciones y
        // el «Jugador de prueba» en los entrenamientos.
        IEnumerable<string> Reserve(int count, bool training) =>
            count < 2 ? new[] { training ? Domain.Entities.Player.TestName : Domain.Entities.Player.ReserveName } : Array.Empty<string>();
        RelationDto Rel(RelationStat r) =>
            new(r.PlayerId, Name(r.PlayerId), r.Played, r.Won, r.Lost, r.WinRate, r.TantosFor, r.TantosAgainst);

        var recent = matches.OrderByDescending(m => m.SortDate ?? DateTime.MinValue).ThenByDescending(m => m.MatchId).Take(RecentCount).Select(m =>
        {
            var home = m.HomePlayerIds.Contains(playerId);
            var mine = home ? m.HomePlayerIds : m.AwayPlayerIds;
            var theirs = home ? m.AwayPlayerIds : m.HomePlayerIds;
            // Los sets se muestran siempre desde el punto de vista del jugador.
            var sets = m.Sets.Select(s => home ? new SetScoreDto(s.Home, s.Away) : new SetScoreDto(s.Away, s.Home)).ToList();
            return new RecentMatchDto(
                m.MatchId, m.CompetitionId, m.CompetitionName,
                string.Join(" / ", mine.Where(x => x != playerId).Select(Name).Concat(Reserve(mine.Count, m.IsTraining))),
                string.Join(" / ", theirs.Select(Name).Concat(Reserve(theirs.Count, m.IsTraining))),
                sets, home == m.HomeWon, m.IsTraining, m.SortDate, m.Ball);
        }).ToList();

        // Lo jugado en el mes actual (hora de Madrid), para el resumen de la portada.
        var (monthFrom, monthTo) = CurrentMonthUtc();
        var monthMatches = matches.Where(m => m.SortDate >= monthFrom && m.SortDate < monthTo).ToList();
        var monthStats = monthMatches.Count == 0 ? null : _calculator.Calculate(playerId, monthMatches);

        return new PlayerHistoryDto(
            ToRow(player.Id, player.Name, player.Position, stats.Played == 0 ? null : stats),
            stats.Partners.Select(Rel).ToList(), stats.Rivals.Select(Rel).ToList(), recent,
            ToRow(player.Id, player.Name, player.Position, monthStats is { Played: > 0 } ? monthStats : null));
    }

    private static (DateTime From, DateTime To) CurrentMonthUtc()
    {
        TimeZoneInfo zone;
        try { zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid"); } catch (TimeZoneNotFoundException) { zone = TimeZoneInfo.Utc; }
        var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
        var start = new DateTime(local.Year, local.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
        return (TimeZoneInfo.ConvertTimeToUtc(start, zone), TimeZoneInfo.ConvertTimeToUtc(start.AddMonths(1), zone));
    }

    private static PlayerHistoryRowDto ToRow(int id, string name, Domain.Enums.PlayerPosition pos, PlayerStats? s) =>
        s is null
            ? new(id, name, pos, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)
            : new(id, name, pos, s.Played, s.Won, s.Lost, s.WinRate, s.SetsFor, s.SetsAgainst,
                s.TantosFor, s.TantosAgainst, s.TantosDiff, s.ClassificationPoints, s.Competitions);
}
