namespace Frontenis.Application.Interfaces.Security;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
    /// <summary>True si el hash usa parámetros antiguos y conviene regenerarlo tras un inicio de sesión correcto.</summary>
    bool NeedsRehash(string hash);
}
