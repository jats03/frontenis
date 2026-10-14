using Frontenis.Domain.Entities;

namespace Frontenis.Application.Interfaces.Security;

public interface ITokenService
{
    (string Token, DateTime ExpiresAtUtc) Create(User user);
}
