using Frontenis.Application.Dtos;

namespace Frontenis.Application.Interfaces.Services;

/// <summary>Consultas públicas (solo lectura) sobre jugadores.</summary>
public interface IPlayerQueries
{
    /// <summary>Lista todos los jugadores.</summary>
    Task<IReadOnlyList<PlayerDto>> ListAsync(CancellationToken ct = default);
}
