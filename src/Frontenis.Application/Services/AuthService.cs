using Frontenis.Application.Abstractions;
using Frontenis.Application.Dtos;
using Frontenis.Domain.Exceptions;

namespace Frontenis.Application.Services;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, string clientIp, CancellationToken ct = default);
    Task ChangePasswordAsync(int userId, ChangePasswordRequest request, string clientIp, CancellationToken ct = default);
}

public sealed class UnauthorizedAppException : Exception
{
    public UnauthorizedAppException(string message) : base(message) { }
}

/// <summary>Demasiados intentos fallidos: hay que esperar <see cref="RetryAfter"/>.</summary>
public sealed class TooManyAttemptsException : Exception
{
    public TimeSpan RetryAfter { get; }
    public TooManyAttemptsException(TimeSpan retryAfter)
        : base($"Demasiados intentos. Inténtalo de nuevo en {Math.Max(1, (int)Math.Ceiling(retryAfter.TotalMinutes))} minuto(s).")
    {
        RetryAfter = retryAfter;
    }
}

/// <summary>Requisitos mínimos de contraseña de administrador.</summary>
public static class PasswordPolicy
{
    public const int MinLength = 12;
    public const int MaxLength = 128;

    private static readonly HashSet<string> Common = new(StringComparer.OrdinalIgnoreCase)
    {
        "password1234", "contraseña123", "123456789012", "admin1234567", "qwertyuiop12", "frontenis1234", "administrador",
    };

    /// <summary>Devuelve el motivo del rechazo, o null si la contraseña es aceptable.</summary>
    public static string? Validate(string? password, string username)
    {
        if (string.IsNullOrEmpty(password) || password.Length < MinLength)
            return $"La contraseña debe tener al menos {MinLength} caracteres.";
        if (password.Length > MaxLength) return $"La contraseña no puede superar los {MaxLength} caracteres.";
        if (password.Distinct().Count() < 5) return "La contraseña es demasiado repetitiva.";
        if (!string.IsNullOrWhiteSpace(username) && password.Contains(username, StringComparison.OrdinalIgnoreCase))
            return "La contraseña no puede contener el nombre de usuario.";
        if (Common.Contains(password)) return "Esa contraseña es demasiado común.";
        var kinds = new[] { password.Any(char.IsLower), password.Any(char.IsUpper), password.Any(char.IsDigit), password.Any(c => !char.IsLetterOrDigit(c)) };
        if (kinds.Count(k => k) < 2 && password.Length < 16)
            return "Combina mayúsculas, minúsculas, números o símbolos (o usa una frase de 16 caracteres o más).";
        return null;
    }
}

public sealed class AuthService : IAuthService
{
    private const int MaxUserFailures = 5;
    private const int MaxIpFailures = 20;

    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokens;
    private readonly ILoginThrottle _throttle;

    // Hash de relleno: si el usuario no existe, se verifica contra él para que el tiempo de respuesta
    // no revele qué usuarios existen.
    private static string? _dummyHash;
    private static readonly object DummyLock = new();

    public AuthService(IUserRepository users, IPasswordHasher hasher, ITokenService tokens, ILoginThrottle throttle)
    {
        _users = users;
        _hasher = hasher;
        _tokens = tokens;
        _throttle = throttle;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, string clientIp, CancellationToken ct = default)
    {
        var username = Normalize(request.Username);
        var password = request.Password ?? string.Empty;
        var userKey = "u:" + username.ToLowerInvariant();
        var ipKey = "ip:" + clientIp;
        EnsureNotLocked(userKey, ipKey);

        var user = password.Length <= PasswordPolicy.MaxLength
            ? await _users.GetByUsernameAsync(username, ct)
            : null;
        var valid = user is not null
            ? _hasher.Verify(password, user.PasswordHash)
            : _hasher.Verify(password, DummyHash());

        if (!valid || user is null)
        {
            _throttle.RegisterFailure(userKey, MaxUserFailures);
            _throttle.RegisterFailure(ipKey, MaxIpFailures);
            throw new UnauthorizedAppException("Usuario o contraseña incorrectos.");
        }

        _throttle.Reset(userKey);
        if (_hasher.NeedsRehash(user.PasswordHash))
            await _users.SetPasswordHashAsync(user.Id, _hasher.Hash(password), ct);

        var (token, expires) = _tokens.Create(user);
        return new LoginResponse(token, expires, user.Username, user.Role);
    }

    public async Task ChangePasswordAsync(int userId, ChangePasswordRequest request, string clientIp, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct) ?? throw new UnauthorizedAppException("Sesión no válida.");
        var userKey = "u:" + user.Username.ToLowerInvariant();
        var ipKey = "ip:" + clientIp;
        EnsureNotLocked(userKey, ipKey);

        var current = request.CurrentPassword ?? string.Empty;
        if (current.Length > PasswordPolicy.MaxLength || !_hasher.Verify(current, user.PasswordHash))
        {
            _throttle.RegisterFailure(userKey, MaxUserFailures);
            _throttle.RegisterFailure(ipKey, MaxIpFailures);
            // 400 y no 401: un 401 haría que la web cerrase la sesión del administrador.
            throw new DomainException("La contraseña actual no es correcta.");
        }

        var problem = PasswordPolicy.Validate(request.NewPassword, user.Username);
        if (problem is not null) throw new DomainException(problem);
        if (request.NewPassword == current) throw new DomainException("La nueva contraseña debe ser distinta de la actual.");

        await _users.SetPasswordHashAsync(user.Id, _hasher.Hash(request.NewPassword), ct);
        _throttle.Reset(userKey);
    }

    private void EnsureNotLocked(params string[] keys)
    {
        foreach (var key in keys)
            if (_throttle.LockedFor(key) is { } wait) throw new TooManyAttemptsException(wait);
    }

    private static string Normalize(string? username)
    {
        var u = (username ?? string.Empty).Trim();
        return u.Length > 100 ? u[..100] : u;
    }

    private string DummyHash()
    {
        if (_dummyHash is not null) return _dummyHash;
        lock (DummyLock) return _dummyHash ??= _hasher.Hash(Guid.NewGuid().ToString("N"));
    }
}
