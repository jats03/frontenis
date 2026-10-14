using Frontenis.Domain.Models;

namespace Frontenis.Domain.Interfaces;

/// <summary>Genera el cuadro de eliminatorias.</summary>
public interface IBracketGenerator
{
    /// <summary>Construye el cuadro de eliminatorias para <paramref name="participantCount"/> clasificados (rondas, cruces y byes).</summary>
    BracketPlan Generate(int participantCount);
}
