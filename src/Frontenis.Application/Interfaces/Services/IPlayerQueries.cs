using Frontenis.Application.Dtos;

namespace Frontenis.Application.Interfaces.Services;

public interface IPlayerQueries
{
    Task<IReadOnlyList<PlayerDto>> ListAsync(CancellationToken ct = default);
}
