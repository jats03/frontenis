using Frontenis.Domain.Entities;

namespace Frontenis.Application.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default);
    Task<User?> GetByIdAsync(int id, CancellationToken ct = default);
    Task SetPasswordHashAsync(int id, string passwordHash, CancellationToken ct = default);
}
