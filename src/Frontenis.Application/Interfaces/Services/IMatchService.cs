using Frontenis.Application.Dtos;

namespace Frontenis.Application.Interfaces.Services;

public interface IMatchService
{
    Task<MatchDto> SetResultAsync(int matchId, SetResultRequest request, CancellationToken ct = default);
    Task<MatchDto> ClearResultAsync(int matchId, CancellationToken ct = default);
}
