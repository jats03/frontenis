using Frontenis.Domain.Models;

namespace Frontenis.Domain.Interfaces;

public interface IBracketGenerator
{
    BracketPlan Generate(int participantCount);
}
