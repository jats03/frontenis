using Frontenis.Application.Dtos;

namespace Frontenis.Application.Interfaces.Services;

/// <summary>Gestión de sesiones de entrenamiento y de sus partidos.</summary>
public interface ITrainingCommands
{
    /// <summary>Crea una sesión de entrenamiento.</summary>
    Task<TrainingSummaryDto> CreateAsync(CreateTrainingRequest request, CancellationToken ct = default);
    /// <summary>Cambia el nombre de la sesión.</summary>
    Task<TrainingSummaryDto> RenameAsync(int id, RenameTrainingRequest request, CancellationToken ct = default);
    /// <summary>Elimina la sesión con todos sus partidos.</summary>
    Task DeleteAsync(int id, CancellationToken ct = default);
    /// <summary>Registra un partido (dos parejas, tantos objetivo y marcador) y devuelve la sesión actualizada.</summary>
    /// <exception cref="Frontenis.Domain.Exceptions.NotFoundException">Algún jugador no existe.</exception>
    Task<TrainingDetailDto> AddMatchAsync(int id, AddTrainingMatchRequest request, CancellationToken ct = default);
    /// <summary>Corrige un partido ya guardado (jugadores, marcador y bola) y devuelve la sesión actualizada; el orden y la hora no cambian.</summary>
    /// <exception cref="Frontenis.Domain.Exceptions.NotFoundException">El entreno, el partido o algún jugador no existe.</exception>
    /// <exception cref="Frontenis.Domain.Exceptions.DomainException">Jugadores repetidos o faltantes, marcador, empate o bola no válidos.</exception>
    Task<TrainingDetailDto> UpdateMatchAsync(int id, int matchId, UpdateTrainingMatchRequest request, CancellationToken ct = default);
    /// <summary>Elimina un partido de la sesión y devuelve la sesión actualizada.</summary>
    Task<TrainingDetailDto> RemoveMatchAsync(int id, int matchId, CancellationToken ct = default);
}
