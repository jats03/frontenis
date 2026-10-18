using Frontenis.Application.Models;

namespace Frontenis.Api.Auditing.Interfaces;

/// <summary>Averigua quién hace una petición: administrador, jugador del dispositivo vinculado o visitante.</summary>
public interface IActorResolver
{
    /// <summary>Identifica al autor; nunca falla (si algo no se puede averiguar, es un visitante).</summary>
    Task<RequestActor> ResolveAsync(HttpContext context, CancellationToken ct = default);
}
