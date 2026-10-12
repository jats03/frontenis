using Frontenis.Application.Abstractions;
using Frontenis.Application.Dtos;

namespace Frontenis.Application.Services;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
}

public sealed class UnauthorizedAppException : Exception
{
    public UnauthorizedAppException(string message) : base(message) { }
}

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokens;

    public AuthService(IUserRepository users, IPasswordHasher hasher, ITokenService tokens)
    {
        _users = users;
        _hasher = hasher;
        _tokens = tokens;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await _users.GetByUsernameAsync(request.Username ?? string.Empty, ct);
        if (user is null || !_hasher.Verify(request.Password ?? string.Empty, user.PasswordHash))
            throw new UnauthorizedAppException("Usuario o contraseña incorrectos.");
        var (token, expires) = _tokens.Create(user);
        return new LoginResponse(token, expires, user.Username, user.Role);
    }
}
