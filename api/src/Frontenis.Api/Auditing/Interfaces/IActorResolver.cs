using Frontenis.Application.Models;

namespace Frontenis.Api.Auditing.Interfaces;

/// <summary>Averigua quién hace una petición: administrador, jugador del dispositivo vinculado o visitante.</summary>
public interface IActorResolver
{
    /// <summary>
    /// Identifica al autor; nunca falla (si algo no se puede averiguar, es un visitante).
    /// Con <paramref name="lookup"/> en falso nunca consulta la base de datos (así una lectura no despierta Neon):
    /// el administrador se reconoce por su sesión y un jugador solo si ya se le identificó hace poco.
    /// </summary>
    Task<RequestActor> ResolveAsync(HttpContext context, bool lookup, CancellationToken ct = default);
}
