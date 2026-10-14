namespace Frontenis.Application.Interfaces.Security;

/// <summary>Cálculo y verificación de hashes de contraseña.</summary>
public interface IPasswordHasher
{
    /// <summary>Calcula el hash seguro (con sal) de la contraseña.</summary>
    string Hash(string password);
    /// <summary>Comprueba que la contraseña corresponde al hash.</summary>
    bool Verify(string password, string hash);
    /// <summary>True si el hash usa parámetros antiguos y conviene regenerarlo tras un inicio de sesión correcto.</summary>
    bool NeedsRehash(string hash);
}
