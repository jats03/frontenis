namespace Frontenis.Application.Dtos;

public sealed record LoginRequest(string Username, string Password);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public sealed record LoginResponse(string Token, DateTime ExpiresAtUtc, string Username, string Role);
