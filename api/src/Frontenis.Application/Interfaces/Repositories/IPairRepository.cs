using Frontenis.Domain.Entities;

namespace Frontenis.Application.Interfaces.Repositories;

/// <summary>Acceso a datos de parejas.</summary>
public interface IPairRepository
{
    /// <summary>Añade una pareja nueva (se persiste con <see cref="IUnitOfWork.SaveChangesAsync"/>).</summary>
    Task AddAsync(Pair pair, CancellationToken ct = default);
}
