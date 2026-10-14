using Frontenis.Domain.Entities;

namespace Frontenis.Application.Interfaces.Repositories;

/// <summary>Acceso a datos de usuarios (administrador).</summary>
public interface IUserRepository
{
    /// <summary>Busca un usuario por su nombre de usuario, o <c>null</c> si no existe.</summary>
    Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default);
    /// <summary>Busca un usuario por su identificador, o <c>null</c> si no existe.</summary>
    Task<User?> GetByIdAsync(int id, CancellationToken ct = default);
    /// <summary>Sustituye el hash de contraseña del usuario y lo guarda inmediatamente.</summary>
    Task SetPasswordHashAsync(int id, string passwordHash, CancellationToken ct = default);
}
