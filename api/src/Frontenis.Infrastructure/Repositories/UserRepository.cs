using Frontenis.Application.Interfaces.Repositories;
using Frontenis.Domain.Entities;
using Frontenis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Frontenis.Infrastructure.Repositories;

/// <summary>Repositorio EF Core de usuarios.</summary>
public sealed class UserRepository : IUserRepository
{
    private readonly FrontenisDbContext _db;
    public UserRepository(FrontenisDbContext db) { _db = db; }

    public Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default) =>
        _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower(), ct);

    public Task<User?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task SetPasswordHashAsync(int id, string passwordHash, CancellationToken ct = default) =>
        _db.Users.Where(u => u.Id == id).ExecuteUpdateAsync(s => s.SetProperty(u => u.PasswordHash, passwordHash), ct);
}
