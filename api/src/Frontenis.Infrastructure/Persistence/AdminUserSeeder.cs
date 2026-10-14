using Frontenis.Application.Interfaces.Security;
using Frontenis.Application.Security;
using Frontenis.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Frontenis.Infrastructure.Persistence;

/// <summary>Siembra el usuario administrador la primera vez, con la contraseña de la configuración.</summary>
public sealed class AdminUserSeeder
{
    private readonly FrontenisDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IConfiguration _config;

    public AdminUserSeeder(FrontenisDbContext db, IPasswordHasher hasher, IConfiguration config)
    {
        _db = db;
        _hasher = hasher;
        _config = config;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var username = _config["Admin:Username"] ?? "admin";
        if (await _db.Users.AnyAsync(u => u.Username == username, ct)) return;

        var password = _config["Admin:Password"] ?? throw new InvalidOperationException("Falta Admin:Password en la configuración.");
        var weak = PasswordPolicy.Validate(password, username);
        if (weak is not null)
            throw new InvalidOperationException($"Admin:Password no es segura ({weak}) Cámbiala en la configuración antes del primer arranque.");

        _db.Users.Add(new User(username, _hasher.Hash(password), Roles.Admin));
        await _db.SaveChangesAsync(ct);
    }
}
