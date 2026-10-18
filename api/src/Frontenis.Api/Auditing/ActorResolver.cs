using System.Security.Claims;
using Frontenis.Api.Auditing.Interfaces;
using Frontenis.Application.Interfaces.Repositories;
using Frontenis.Application.Interfaces.Services;
using Frontenis.Application.Models;
using Frontenis.Application.Security;
using Frontenis.Domain.Enums;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Memory;

namespace Frontenis.Api.Auditing;

/// <summary>
/// Implementación de <see cref="IActorResolver"/>. El administrador se reconoce por su sesión; un jugador, por el
/// identificador de su dispositivo (solo se consulta la BD al registrar una escritura y se recuerda un rato).
/// </summary>
public sealed class ActorResolver : IActorResolver
{
    public const string DeviceHeader = "X-Device-Token";
    private static readonly TimeSpan Remember = TimeSpan.FromMinutes(30);

    private readonly IDeviceAccessResolver _devices;
    private readonly IPlayerRepository _players;
    private readonly IMemoryCache _cache;

    public ActorResolver(IDeviceAccessResolver devices, IPlayerRepository players, IMemoryCache cache)
    {
        _devices = devices;
        _players = players;
        _cache = cache;
    }

    public async Task<RequestActor> ResolveAsync(HttpContext context, bool lookup, CancellationToken ct = default)
    {
        try
        {
            var user = context.User;
            if (user.Identity?.IsAuthenticated != true && context.Request.Headers.ContainsKey("Authorization"))
                user = (await context.AuthenticateAsync()).Principal ?? user;
            if (user.Identity?.IsAuthenticated == true && user.IsInRole(Roles.Admin))
                return new RequestActor(ActorKind.Admin, AuditText.Clean(user.FindFirstValue(ClaimTypes.Name) ?? "admin", 100), null);

            var token = context.Request.Headers[DeviceHeader].ToString();
            if (string.IsNullOrWhiteSpace(token)) return RequestActor.Visitor;
            // La clave en memoria es el propio identificador, que solo vive en el servidor un rato y nunca se escribe.
            var key = "actor:" + token;
            if (!lookup) return _cache.TryGetValue(key, out RequestActor? known) && known is not null ? known : RequestActor.Visitor;
            return await _cache.GetOrCreateAsync(key, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = Remember;
                var access = await _devices.FindAsync(token, ct);
                if (access is null) return RequestActor.Visitor;
                var player = await _players.GetByIdAsync(access.OwnerPlayerId, ct);
                return player is null ? RequestActor.Visitor
                    : new RequestActor(ActorKind.Player, AuditText.Clean(player.Name, 100), player.Id);
            }) ?? RequestActor.Visitor;
        }
        catch (Exception) when (!ct.IsCancellationRequested)
        {
            return RequestActor.Visitor; // el registro nunca debe romper la petición
        }
    }
}
