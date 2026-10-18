using Frontenis.Domain.Enums;

namespace Frontenis.Application.Models;

/// <summary>Quién hace una petición, para el registro de actividad.</summary>
public sealed record RequestActor(ActorKind Kind, string Name, int? PlayerId)
{
    public static readonly RequestActor Visitor = new(ActorKind.Visitor, "visitante", null);
}
