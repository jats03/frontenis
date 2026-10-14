namespace Frontenis.Domain.Entities;

/// <summary>Usuario con acceso de administración.</summary>
public sealed class User
{
    private User() { Username = string.Empty; PasswordHash = string.Empty; Role = string.Empty; }

    public User(string username, string passwordHash, string role)
    {
        Username = username;
        PasswordHash = passwordHash;
        Role = role;
    }

    public int Id { get; private set; }
    public string Username { get; private set; }
    public string PasswordHash { get; private set; }
    public string Role { get; private set; }
}
