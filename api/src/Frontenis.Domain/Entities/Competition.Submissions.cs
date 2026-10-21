using Frontenis.Domain.Exceptions;
using Frontenis.Domain.Models;

namespace Frontenis.Domain.Entities;

public sealed partial class Competition
{
    /// <summary>
    /// Comprueba que un jugador puede enviar este resultado (partido con las dos parejas, sin resultado y en una
    /// competición que lo admite) y que el marcador cumple las reglas del formato. No cambia nada.
    /// </summary>
    /// <exception cref="DomainException">Partido no editable, ya jugado o marcador no válido.</exception>
    public Match EnsureSubmittable(int matchId, IReadOnlyList<SetScore> sets)
    {
        var match = FindMatch(matchId);
        EnsureEditable(match);
        if (!match.IsReady) throw new DomainException("El partido aún no tiene las dos parejas definidas.");
        if (match.IsPlayed) throw new DomainException("Este partido ya tiene resultado.");
        Match.ValidateSets(sets, SetsPerMatch);
        return match;
    }
}
