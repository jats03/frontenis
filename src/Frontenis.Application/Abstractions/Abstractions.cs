using Frontenis.Domain.Entities;

namespace Frontenis.Application.Abstractions;

public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken ct = default);
}

public interface IPlayerRepository
{
    Task<IReadOnlyList<Player>> ListAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Player>> GetByIdsAsync(IReadOnlyCollection<int> ids, CancellationToken ct = default);
    Task<Player?> GetByIdAsync(int id, CancellationToken ct = default);
    Task AddAsync(Player player, CancellationToken ct = default);
    void Remove(Player player);
    Task<bool> IsInUseAsync(int playerId, CancellationToken ct = default);
}

public interface IPairRepository
{
    Task AddAsync(Pair pair, CancellationToken ct = default);
}

public interface ICompetitionRepository
{
    Task<IReadOnlyList<Competition>> ListAsync(CancellationToken ct = default);
    /// <summary>Carga el agregado completo (grupos, parejas con jugadores y partidos).</summary>
    Task<Competition?> GetDetailedAsync(int id, CancellationToken ct = default);
    Task<Competition?> GetDetailedByMatchIdAsync(int matchId, CancellationToken ct = default);
    Task AddAsync(Competition competition, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default);
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public interface ITokenService
{
    (string Token, DateTime ExpiresAtUtc) Create(User user);
}

public static class Roles
{
    public const string Admin = "Admin";
}
