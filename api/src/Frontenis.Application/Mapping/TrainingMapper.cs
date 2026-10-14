using Frontenis.Application.Dtos;
using Frontenis.Domain.Entities;
using Frontenis.Domain.Models;

namespace Frontenis.Application.Mapping;

/// <summary>Conversión de entidades de entrenamiento a DTO.</summary>
internal static class TrainingMapper
{
    public static TrainingSummaryDto ToSummary(this TrainingSession s) =>
        new(s.Id, s.Name, s.SessionDate, s.PointsPerWin, s.PointsPerLoss, s.Matches.Count);

    /// <summary>Partidos de la sesión en el formato que consumen las clasificaciones del dominio.</summary>
    public static List<TrainingGame> ToGames(this TrainingSession s) =>
        s.Matches.OrderBy(m => m.Order).ThenBy(m => m.Id)
            .Select(m => new TrainingGame(m.Home1Id, m.Home2Id, m.Away1Id, m.Away2Id, m.HomeScore, m.AwayScore))
            .ToList();
}
