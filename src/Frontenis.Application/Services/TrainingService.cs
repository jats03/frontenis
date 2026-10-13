using Frontenis.Application.Abstractions;
using Frontenis.Application.Dtos;
using Frontenis.Domain.Entities;
using Frontenis.Domain.Exceptions;
using Frontenis.Domain.Services;

namespace Frontenis.Application.Services;

public interface ITrainingService
{
    Task<IReadOnlyList<TrainingSummaryDto>> ListAsync(CancellationToken ct = default);
    Task<TrainingDetailDto> GetAsync(int id, CancellationToken ct = default);
    Task<TrainingSummaryDto> CreateAsync(CreateTrainingRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task<TrainingSummaryDto> RenameAsync(int id, RenameTrainingRequest request, CancellationToken ct = default);
    Task<TrainingDetailDto> AddMatchAsync(int id, AddTrainingMatchRequest request, CancellationToken ct = default);
    Task<TrainingDetailDto> RemoveMatchAsync(int id, int matchId, CancellationToken ct = default);
    Task<IReadOnlyList<TrainingSuggestionDto>> SuggestAsync(int id, TrainingSuggestRequest request, CancellationToken ct = default);
}

public sealed class TrainingService : ITrainingService
{
    private const int MaxSuggestions = 10;
    private const int MaxPresent = 30; // la búsqueda crece con C(n,4): se acota

    private readonly ITrainingRepository _trainings;
    private readonly IPlayerRepository _players;
    private readonly ITrainingPlanner _planner;
    private readonly IUnitOfWork _uow;
    private readonly IReadCache _cache;

    public TrainingService(ITrainingRepository trainings, IPlayerRepository players, ITrainingPlanner planner, IUnitOfWork uow, IReadCache cache)
    {
        _cache = cache;
        _trainings = trainings; _players = players; _planner = planner; _uow = uow;
    }

    public async Task<IReadOnlyList<TrainingSummaryDto>> ListAsync(CancellationToken ct = default) =>
        await _cache.GetOrAddAsync<IReadOnlyList<TrainingSummaryDto>>("trainings:list",
            async () => (await _trainings.ListAsync(CancellationToken.None)).Select(Summary).ToList());

    public async Task<TrainingDetailDto> GetAsync(int id, CancellationToken ct = default) =>
        await _cache.GetOrAddAsync($"trainings:{id}", async () => await DetailAsync(await LoadAsync(id, CancellationToken.None), CancellationToken.None));

    public async Task<TrainingSummaryDto> CreateAsync(CreateTrainingRequest r, CancellationToken ct = default)
    {
        var session = TrainingSession.Create(r.Name, r.Date, r.PointsPerWin, r.PointsPerLoss);
        await _trainings.AddAsync(session, ct);
        await _uow.SaveChangesAsync(ct);
        return Summary(session);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        _trainings.Remove(await LoadAsync(id, ct));
        await _uow.SaveChangesAsync(ct);
    }

    public async Task<TrainingSummaryDto> RenameAsync(int id, RenameTrainingRequest r, CancellationToken ct = default)
    {
        var session = await LoadAsync(id, ct);
        session.Rename(r.Name);
        await _uow.SaveChangesAsync(ct);
        return Summary(session);
    }

    public async Task<TrainingDetailDto> AddMatchAsync(int id, AddTrainingMatchRequest r, CancellationToken ct = default)
    {
        var session = await LoadAsync(id, ct);
        var ids = new[] { r.Home1Id, r.Home2Id, r.Away1Id, r.Away2Id };
        var found = await _players.GetByIdsAsync(ids.Distinct().ToList(), ct);
        if (found.Count != ids.Distinct().Count()) throw new NotFoundException("Algún jugador no existe.");
        session.AddMatch(r.Home1Id, r.Home2Id, r.Away1Id, r.Away2Id, r.TargetPoints, r.HomeScore, r.AwayScore);
        await _uow.SaveChangesAsync(ct);
        return await DetailAsync(session, ct);
    }

    public async Task<TrainingDetailDto> RemoveMatchAsync(int id, int matchId, CancellationToken ct = default)
    {
        var session = await LoadAsync(id, ct);
        session.RemoveMatch(matchId);
        await _uow.SaveChangesAsync(ct);
        return await DetailAsync(session, ct);
    }

    public async Task<IReadOnlyList<TrainingSuggestionDto>> SuggestAsync(int id, TrainingSuggestRequest r, CancellationToken ct = default)
    {
        var session = await LoadAsync(id, ct);
        var present = (r.PlayerIds ?? Array.Empty<int>()).Distinct().ToList();
        if (present.Count < 4) throw new DomainException("Marca al menos 4 jugadores presentes.");
        if (present.Count > MaxPresent) throw new DomainException($"Máximo {MaxPresent} jugadores presentes.");
        var names = (await _players.ListAsync(ct)).ToDictionary(p => p.Id, p => p.Name);
        if (present.Any(p => !names.ContainsKey(p))) throw new NotFoundException("Algún jugador no existe.");

        string Pair(int a, int b) => $"{names[a]} / {names[b]}";
        return _planner.Suggest(present, Games(session), Math.Clamp(r.Count, 1, MaxSuggestions))
            .Select(s => new TrainingSuggestionDto(
                new[] { s.Home1, s.Home2 }, new[] { s.Away1, s.Away2 }, Pair(s.Home1, s.Home2), Pair(s.Away1, s.Away2),
                s.HomeLevel, s.AwayLevel, s.Balance, s.Reason))
            .ToList();
    }

    // ---------- helpers ----------
    private async Task<TrainingSession> LoadAsync(int id, CancellationToken ct) =>
        await _trainings.GetAsync(id, ct) ?? throw new NotFoundException("Entrenamiento no encontrado.");

    private static TrainingSummaryDto Summary(TrainingSession s) =>
        new(s.Id, s.Name, s.SessionDate, s.PointsPerWin, s.PointsPerLoss, s.Matches.Count);

    private static List<TrainingGame> Games(TrainingSession s) =>
        s.Matches.OrderBy(m => m.Order).ThenBy(m => m.Id)
            .Select(m => new TrainingGame(m.Home1Id, m.Home2Id, m.Away1Id, m.Away2Id, m.HomeScore, m.AwayScore, m.TargetPoints))
            .ToList();

    private async Task<TrainingDetailDto> DetailAsync(TrainingSession s, CancellationToken ct)
    {
        var names = (await _players.ListAsync(ct)).ToDictionary(p => p.Id, p => p.Name);
        string N(int id) => names.GetValueOrDefault(id, "—");
        var games = Games(s);

        TrainingRowDto Row(TrainingRow r) => new(
            r.Rank, r.Player2Id == 0 ? N(r.Player1Id) : $"{N(r.Player1Id)} / {N(r.Player2Id)}",
            r.Player2Id == 0 ? new[] { r.Player1Id } : new[] { r.Player1Id, r.Player2Id },
            r.Played, r.Won, r.Lost, r.PointsFor, r.PointsAgainst, r.PointsDiff, r.ClassificationPoints);

        return new TrainingDetailDto(
            Summary(s),
            s.Matches.OrderBy(m => m.Order).ThenBy(m => m.Id).Select(m => new TrainingMatchDto(
                m.Id, m.Order, $"{N(m.Home1Id)} / {N(m.Home2Id)}", $"{N(m.Away1Id)} / {N(m.Away2Id)}",
                new[] { m.Home1Id, m.Home2Id }, new[] { m.Away1Id, m.Away2Id },
                m.TargetPoints, m.HomeScore, m.AwayScore, m.HomeWon)).ToList(),
            TrainingStandings.ByPair(games, s.PointsPerWin, s.PointsPerLoss).Select(Row).ToList(),
            TrainingStandings.ByPlayer(games, s.PointsPerWin, s.PointsPerLoss).Select(Row).ToList());
    }
}
