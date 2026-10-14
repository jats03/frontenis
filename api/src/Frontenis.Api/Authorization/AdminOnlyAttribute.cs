using Frontenis.Application.Security;
using Microsoft.AspNetCore.Authorization;

namespace Frontenis.Api.Authorization;

/// <summary>Restringe el endpoint al rol de administrador.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class AdminOnlyAttribute : AuthorizeAttribute
{
    public AdminOnlyAttribute() { Roles = Frontenis.Application.Security.Roles.Admin; }
}
