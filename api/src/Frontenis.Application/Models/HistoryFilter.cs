using Frontenis.Domain.Entities;
using Frontenis.Domain.Enums;

namespace Frontenis.Application.Models;

/// <summary>Consulta de solo lectura de partidos jugados (histórico de jugadores).</summary>
/// <summary>
/// Origen de los partidos del histórico. Sin ningún id = todo (competiciones y entrenamientos);
/// con ids = solo esas competiciones y/o esos entrenamientos.
/// <para>
/// <see cref="Ball"/> es siempre una sola bola (por defecto la preolímpica): los partidos de entrenamiento solo cuentan
/// si se jugaron con ella. Los partidos de competición no llevan bola y no se ven afectados.
/// </para>
/// </summary>
public sealed record HistoryFilter(
    IReadOnlyCollection<int> CompetitionIds, IReadOnlyCollection<int> TrainingIds, TrainingBall Ball = TrainingMatch.DefaultBall)
{
    public static HistoryFilter All { get; } = new(Array.Empty<int>(), Array.Empty<int>());
    public bool IsAll => CompetitionIds.Count == 0 && TrainingIds.Count == 0;
    public bool IncludesCompetitions => IsAll || CompetitionIds.Count > 0;
    public bool IncludesTrainings => IsAll || TrainingIds.Count > 0;
}
