using Frontenis.Application.Dtos;

namespace Frontenis.Application.Interfaces.Services;

/// <summary>Formación e inscripción de parejas en una competición.</summary>
public interface IPairingService
{
    /// <summary>Calcula un emparejamiento aleatorio sin guardar nada.</summary>
    Task<PairingPreviewDto> PreviewAsync(PlayersSelection selection, CancellationToken ct = default);
    /// <summary>Genera el emparejamiento aleatorio y lo inscribe en la competición.</summary>
    Task<IReadOnlyList<PairDto>> RegisterRandomAsync(int competitionId, PlayersSelection selection, CancellationToken ct = default);
    /// <summary>Inscribe una pareja formada a mano con dos jugadores concretos.</summary>
    /// <exception cref="Frontenis.Domain.Exceptions.DomainException">Jugadores repetidos o ya inscritos en la competición.</exception>
    Task<PairDto> RegisterManualAsync(int competitionId, ManualPairRequest request, CancellationToken ct = default);
}
