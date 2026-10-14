using Frontenis.Application.Models;
using Frontenis.Domain.Models;

namespace Frontenis.Application.Interfaces.Repositories;

public interface IMatchHistoryRepository
{
    Task<IReadOnlyList<PlayedMatch>> ListPlayedAsync(HistoryFilter filter, int? playerId, CancellationToken ct = default);
}
