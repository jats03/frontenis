namespace Frontenis.Application.Models;

/// <summary>Consulta de solo lectura de partidos jugados (histórico de jugadores).</summary>
/// <summary>
/// Origen de los partidos del histórico. Sin ningún id = todo (competiciones y entrenamientos);
/// con ids = solo esas competiciones y/o esos entrenamientos.
/// </summary>
public sealed record HistoryFilter(IReadOnlyCollection<int> CompetitionIds, IReadOnlyCollection<int> TrainingIds)
{
    public static HistoryFilter All { get; } = new(Array.Empty<int>(), Array.Empty<int>());
    public bool IsAll => CompetitionIds.Count == 0 && TrainingIds.Count == 0;
    public bool IncludesCompetitions => IsAll || CompetitionIds.Count > 0;
    public bool IncludesTrainings => IsAll || TrainingIds.Count > 0;
}
