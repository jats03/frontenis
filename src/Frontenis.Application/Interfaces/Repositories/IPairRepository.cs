using Frontenis.Domain.Entities;

namespace Frontenis.Application.Interfaces.Repositories;

public interface IPairRepository
{
    Task AddAsync(Pair pair, CancellationToken ct = default);
}
