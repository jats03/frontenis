using Frontenis.Domain.Entities;
using Frontenis.Domain.Models;

namespace Frontenis.Domain.Interfaces;

public interface IPairingStrategy
{
    PairingResult Generate(IReadOnlyList<Player> players);
}
