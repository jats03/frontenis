using Frontenis.Domain.Entities;
using Frontenis.Domain.Services;

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

/// <summary>Consulta de solo lectura de partidos jugados (histórico de jugadores).</summary>
/// <summary>
/// Origen de los partidos del histórico. Sin ningún id = todo (competiciones y entrenamientos);
/// con ids = solo esas competiciones y/o esos entrenamientos.
/// </summary>
public sealed record HistoryFilter(IReadOnlyCollection<int> CompetitionIds, IReadOnlyCollection<int> TrainingIds)
{
    public static HistoryFilter All { get; } = new(Array.Empty<int>(), Array.Empty<int>());
    public bool IsAll => CompetitionIds.Count == 0 && TrainingIds.Count == 0;
    public bool IncludesCompetitions => IsAll || CompetitionIds.Count > 0;
    public bool IncludesTrainings => IsAll || TrainingIds.Count > 0;
}

public interface IMatchHistoryRepository
{
    Task<IReadOnlyList<PlayedMatch>> ListPlayedAsync(HistoryFilter filter, int? playerId, CancellationToken ct = default);
}

public interface ITrainingRepository
{
    Task<IReadOnlyList<TrainingSession>> ListAsync(CancellationToken ct = default);
    /// <summary>Carga la sesión con todos sus partidos.</summary>
    Task<TrainingSession?> GetAsync(int id, CancellationToken ct = default);
    Task AddAsync(TrainingSession session, CancellationToken ct = default);
    void Remove(TrainingSession session);
}

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default);
    Task<User?> GetByIdAsync(int id, CancellationToken ct = default);
    Task SetPasswordHashAsync(int id, string passwordHash, CancellationToken ct = default);
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
    /// <summary>True si el hash usa parámetros antiguos y conviene regenerarlo tras un inicio de sesión correcto.</summary>
    bool NeedsRehash(string hash);
}

public interface ITokenService
{
    (string Token, DateTime ExpiresAtUtc) Create(User user);
}

/// <summary>Limita los intentos fallidos de inicio de sesión (por usuario y por IP).</summary>
public interface ILoginThrottle
{
    /// <summary>Tiempo que falta para poder reintentar, o null si no hay bloqueo.</summary>
    TimeSpan? LockedFor(string key);
    void RegisterFailure(string key, int maxFailures);
    void Reset(string key);
}

public static class Roles
{
    public const string Admin = "Admin";
}
