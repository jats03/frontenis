using Frontenis.Application.Dtos;

namespace Frontenis.Application.Interfaces.Services;

public interface IPlayerCommands
{
    Task<PlayerDto> CreateAsync(PlayerRequest request, CancellationToken ct = default);
    Task<PlayerDto> UpdateAsync(int id, PlayerRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
