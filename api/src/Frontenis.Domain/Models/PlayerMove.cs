namespace Frontenis.Domain.Models;

/// <summary>Un jugador que cambia de grupo al terminar la liga (índices de grupo: 0 = el de más nivel).</summary>
public sealed record PlayerMove(int PlayerId, int FromGroupIndex, int ToGroupIndex)
{
    public bool IsPromotion => ToGroupIndex < FromGroupIndex;
}
