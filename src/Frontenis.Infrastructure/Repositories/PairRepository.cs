using Frontenis.Application.Interfaces.Repositories;
using Frontenis.Domain.Entities;
using Frontenis.Infrastructure.Persistence;

namespace Frontenis.Infrastructure.Repositories;

public sealed class PairRepository : IPairRepository
{
    private readonly FrontenisDbContext _db;
    public PairRepository(FrontenisDbContext db) { _db = db; }

    public async Task AddAsync(Pair pair, CancellationToken ct = default) => await _db.Pairs.AddAsync(pair, ct);
}
