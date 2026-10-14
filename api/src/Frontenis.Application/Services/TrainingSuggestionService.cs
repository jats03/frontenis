using Frontenis.Application.Dtos;
using Frontenis.Application.Extensions;
using Frontenis.Application.Interfaces.Repositories;
using Frontenis.Application.Interfaces.Services;
using Frontenis.Application.Mapping;
using Frontenis.Domain.Entities;
using Frontenis.Domain.Exceptions;
using Frontenis.Domain.Interfaces;

namespace Frontenis.Application.Services;

/// <summary>Sugiere partidos usando el nivel estimado de los jugadores presentes.</summary>
public sealed class TrainingSuggestionService : ITrainingSuggestionService
{
    private const int MaxSuggestions = 10;
    private const int MaxPresent = 30; // la búsqueda crece con C(n,4): se acota

    private readonly ITrainingRepository _trainings;
    private readonly IPlayerRepository _players;
    private readonly ITrainingPlanner _planner;

    public TrainingSuggestionService(ITrainingRepository trainings, IPlayerRepository players, ITrainingPlanner planner)
    {
        _trainings = trainings;
        _players = players;
        _planner = planner;
    }

    public async Task<IReadOnlyList<TrainingSuggestionDto>> SuggestAsync(int id, TrainingSuggestRequest r, CancellationToken ct = default)
    {
        var session = await _trainings.GetOrThrowAsync(id, ct);
        var present = (r.PlayerIds ?? Array.Empty<int>()).Distinct().ToList();
        if (present.Count < 4) throw new DomainException("Marca al menos 4 jugadores presentes.");
        if (present.Count > MaxPresent) throw new DomainException($"Máximo {MaxPresent} jugadores presentes.");
        var names = (await _players.ListAsync(ct)).ToDictionary(p => p.Id, p => p.Name);
        if (present.Any(p => !names.ContainsKey(p))) throw new NotFoundException("Algún jugador no existe.");

        string Pair(int a, int b) => $"{names[a]} / {names[b]}";
        return _planner.Suggest(present, session.ToGames(), Math.Clamp(r.Count, 1, MaxSuggestions))
            .Select(s => new TrainingSuggestionDto(
                new[] { s.Home1, s.Home2 }, new[] { s.Away1, s.Away2 }, Pair(s.Home1, s.Home2), Pair(s.Away1, s.Away2),
                s.HomeLevel, s.AwayLevel, s.Balance, s.Reason))
            .ToList();
    }
}
