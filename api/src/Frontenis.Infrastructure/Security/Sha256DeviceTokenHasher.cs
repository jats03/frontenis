using System.Security.Cryptography;
using System.Text;
using Frontenis.Application.Interfaces.Security;

namespace Frontenis.Infrastructure.Security;

/// <summary>
/// El dispositivo genera un identificador aleatorio largo (UUID); basta un SHA-256 sin sal porque no es una
/// contraseña elegida por una persona. Así la BD nunca guarda un identificador que sirva para suplantar un móvil.
/// </summary>
public sealed class Sha256DeviceTokenHasher : IDeviceTokenHasher
{
    private const int MinLength = 32;
    private const int MaxLength = 64;

    public bool IsValid(string? token) =>
        token is { Length: >= MinLength and <= MaxLength } && token.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');

    public string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
}
