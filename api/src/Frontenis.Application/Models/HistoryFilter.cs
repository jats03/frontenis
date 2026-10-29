using Frontenis.Domain.Entities;
using Frontenis.Domain.Enums;

namespace Frontenis.Application.Models;

/// <summary>Consulta de solo lectura de partidos jugados (histórico de jugadores).</summary>
/// <summary>
/// Origen de los partidos del histórico. Sin ningún id = todo (competiciones y entrenamientos);
/// con ids = solo esas competiciones y/o esos entrenamientos.
/// <para>
/// <see cref="Balls"/> son los tipos de bola que cuentan: solo se tienen en cuenta los partidos (de entrenamiento y de
/// competición) jugados con alguna de ellas. Vacío o <c>null</c> = todas las bolas, que solo usa la carta de nivel del
/// jugador; la pantalla del histórico envía siempre al menos una (por defecto, la preolímpica).
/// </para>
/// </summary>
public sealed record HistoryFilter(
    IReadOnlyCollection<int> CompetitionIds, IReadOnlyCollection<int> TrainingIds,
    IReadOnlyCollection<BallType>? Balls = null)
{
    public static HistoryFilter All { get; } = new(Array.Empty<int>(), Array.Empty<int>());
    public bool IsAll => CompetitionIds.Count == 0 && TrainingIds.Count == 0;
    public bool IncludesCompetitions => IsAll || CompetitionIds.Count > 0;
    public bool IncludesTrainings => IsAll || TrainingIds.Count > 0;
    /// <summary>Indica si se cuentan todas las bolas (sin filtrar por bola).</summary>
    public bool AllBalls => Balls is null || Balls.Count == 0;
}
