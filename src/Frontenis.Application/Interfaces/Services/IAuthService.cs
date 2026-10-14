using Frontenis.Application.Dtos;

namespace Frontenis.Application.Interfaces.Services;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, string clientIp, CancellationToken ct = default);
    Task ChangePasswordAsync(int userId, ChangePasswordRequest request, string clientIp, CancellationToken ct = default);
}
