using Frontenis.Application.Abstractions;
using Frontenis.Application.Dtos;
using Frontenis.Domain.Exceptions;
using Frontenis.Domain.Services;

namespace Frontenis.Application.Services;

public interface IPlayerHistoryService
{
    /// <summary>Ranking histórico de todos los jugadores (opcionalmente de una sola competición).</summary>
    Task<IReadOnlyList<PlayerHistoryRowDto>> RankingAsync(HistoryFilter filter, CancellationToken ct = default);
    Task<PlayerHistoryDto> PlayerAsync(int playerId, HistoryFilter filter, CancellationToken ct = default);
}

public sealed class PlayerHistoryService : IPlayerHistoryService
{
    private const int RecentCount = 15;

    private readonly IMatchHistoryRepository _history;
    private readonly IPlayerRepository _players;
    private readonly IPlayerStatsCalculator _calculator;
    private readonly IReadCache _cache;

    public PlayerHistoryService(IMatchHistoryRepository history, IPlayerRepository players, IPlayerStatsCalculator calculator, IReadCache cache)
    {
        _cache = cache;
        _history = history;
        _players = players;
        _calculator = calculator;
    }

    private static string Key(HistoryFilter f) =>
        $"c={string.Join(',', f.CompetitionIds.OrderBy(x => x))};t={string.Join(',', f.TrainingIds.OrderBy(x => x))}";

    public Task<IReadOnlyList<PlayerHistoryRowDto>> RankingAsync(HistoryFilter filter, CancellationToken ct = default) =>
        _cache.GetOrAddAsync($"history:ranking:{Key(filter)}", () => ComputeRankingAsync(filter, CancellationToken.None));

    public Task<PlayerHistoryDto> PlayerAsync(int playerId, HistoryFilter filter, CancellationToken ct = default) =>
        _cache.GetOrAddAsync($"history:player:{playerId}:{Key(filter)}", () => ComputePlayerAsync(playerId, filter, CancellationToken.None));

    private async Task<IReadOnlyList<PlayerHistoryRowDto>> ComputeRankingAsync(HistoryFilter filter, CancellationToken ct)
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

    private async Task<PlayerHistoryDto> ComputePlayerAsync(int playerId, HistoryFilter filter, CancellationToken ct)
    {
        var player = await _players.GetByIdAsync(playerId, ct) ?? throw new NotFoundException("Jugador no encontrado.");
        var names = (await _players.ListAsync(ct)).ToDictionary(p => p.Id, p => p.Name);
        var matches = await _history.ListPlayedAsync(filter, playerId, ct);
        var stats = _calculator.Calculate(playerId, matches);

        string Name(int id) => names.GetValueOrDefault(id, "—");
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
                string.Join(" / ", mine.Where(x => x != playerId).Select(Name)),
                string.Join(" / ", theirs.Select(Name)),
                sets, home == m.HomeWon, m.IsTraining);
        }).ToList();

        return new PlayerHistoryDto(
            ToRow(player.Id, player.Name, player.Position, stats.Played == 0 ? null : stats),
            stats.Partners.Select(Rel).ToList(), stats.Rivals.Select(Rel).ToList(), recent);
    }

    private static PlayerHistoryRowDto ToRow(int id, string name, Domain.Enums.PlayerPosition pos, PlayerStats? s) =>
        s is null
            ? new(id, name, pos, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)
            : new(id, name, pos, s.Played, s.Won, s.Lost, s.WinRate, s.SetsFor, s.SetsAgainst,
                s.TantosFor, s.TantosAgainst, s.TantosDiff, s.ClassificationPoints, s.Competitions);
}
