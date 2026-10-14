using Frontenis.Domain.Models;

namespace Frontenis.Domain.Interfaces;

/// <summary>Estima niveles y propone partidos equilibrados para entrenar.</summary>
public interface ITrainingPlanner
{
    /// <summary>Mejores partidos posibles entre los jugadores presentes, del más recomendable al menos.</summary>
    IReadOnlyList<TrainingSuggestion> Suggest(IReadOnlyList<int> present, IReadOnlyList<TrainingGame> games, int count);

    /// <summary>Nivel estimado (0-100) de cada jugador a partir de sus resultados.</summary>
    IReadOnlyDictionary<int, double> Levels(IReadOnlyList<int> players, IReadOnlyList<TrainingGame> games);
}
