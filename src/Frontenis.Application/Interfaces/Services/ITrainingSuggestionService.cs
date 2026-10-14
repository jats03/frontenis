using Frontenis.Application.Dtos;

namespace Frontenis.Application.Interfaces.Services;

public interface ITrainingSuggestionService
{
    /// <summary>Solo lectura (no guarda nada): propone los próximos partidos entre los jugadores presentes.</summary>
    Task<IReadOnlyList<TrainingSuggestionDto>> SuggestAsync(int id, TrainingSuggestRequest request, CancellationToken ct = default);
}
