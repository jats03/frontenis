using Frontenis.Domain.Models;

namespace Frontenis.Domain.Interfaces;

/// <summary>Estima niveles y propone partidos equilibrados para entrenar.</summary>
public interface ITrainingPlanner
{
    /// <summary>
    /// Mejores partidos posibles entre los jugadores presentes, del más recomendable al menos. <paramref name="games"/> son los
    /// partidos de la sesión actual (descanso, equidad y repeticiones); <paramref name="history"/>, todos los partidos de
    /// entrenamiento de todas las sesiones, de los que sale el nivel de cada jugador (si es <c>null</c>, solo los de la sesión).
    /// </summary>
    IReadOnlyList<TrainingSuggestion> Suggest(IReadOnlyList<int> present, IReadOnlyList<TrainingGame> games, int count,
        IReadOnlyList<TrainingGame>? history = null);

    /// <summary>Nivel estimado (0-100) de cada jugador a partir de sus resultados.</summary>
    IReadOnlyDictionary<int, double> Levels(IReadOnlyList<int> players, IReadOnlyList<TrainingGame> games);
}
