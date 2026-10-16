using Frontenis.Application.Dtos;

namespace Frontenis.Application.Interfaces.Services;

/// <summary>Consulta de apadrinados.</summary>
public interface IPlayerDelegateQueries
{
    /// <summary>Lista quién puede apuntar a quién.</summary>
    Task<IReadOnlyList<PlayerDelegateDto>> ListAsync(CancellationToken ct = default);
}
