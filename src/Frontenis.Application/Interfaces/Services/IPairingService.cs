using Frontenis.Application.Dtos;

namespace Frontenis.Application.Interfaces.Services;

public interface IPairingService
{
    /// <summary>Calcula un emparejamiento aleatorio sin guardar nada.</summary>
    Task<PairingPreviewDto> PreviewAsync(PlayersSelection selection, CancellationToken ct = default);
    /// <summary>Genera el emparejamiento aleatorio y lo inscribe en la competición.</summary>
    Task<IReadOnlyList<PairDto>> RegisterRandomAsync(int competitionId, PlayersSelection selection, CancellationToken ct = default);
    Task<PairDto> RegisterManualAsync(int competitionId, ManualPairRequest request, CancellationToken ct = default);
}
