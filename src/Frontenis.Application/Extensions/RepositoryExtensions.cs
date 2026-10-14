using Frontenis.Application.Interfaces.Repositories;
using Frontenis.Domain.Entities;
using Frontenis.Domain.Exceptions;

namespace Frontenis.Application.Extensions;

/// <summary>Cargas "o 404": evitan repetir en cada servicio la comprobación de existencia.</summary>
internal static class RepositoryExtensions
{
    public static async Task<Competition> GetDetailedOrThrowAsync(this ICompetitionRepository repository, int id, CancellationToken ct) =>
        await repository.GetDetailedAsync(id, ct) ?? throw new NotFoundException("Competición no encontrada.");

    public static async Task<Competition> GetDetailedByMatchOrThrowAsync(this ICompetitionRepository repository, int matchId, CancellationToken ct) =>
        await repository.GetDetailedByMatchIdAsync(matchId, ct) ?? throw new NotFoundException("Partido no encontrado.");

    public static async Task<Player> GetOrThrowAsync(this IPlayerRepository repository, int id, CancellationToken ct) =>
        await repository.GetByIdAsync(id, ct) ?? throw new NotFoundException("Jugador no encontrado.");

    public static async Task<TrainingSession> GetOrThrowAsync(this ITrainingRepository repository, int id, CancellationToken ct) =>
        await repository.GetAsync(id, ct) ?? throw new NotFoundException("Entrenamiento no encontrado.");
}
