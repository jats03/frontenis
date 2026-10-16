namespace Frontenis.Application.Interfaces.Security;

/// <summary>Valida y resume (hash) el identificador aleatorio que genera cada dispositivo, para no guardarlo en claro.</summary>
public interface IDeviceTokenHasher
{
    /// <summary>Comprueba que el identificador tiene un formato aceptable (longitud y caracteres).</summary>
    bool IsValid(string? token);
    /// <summary>Hash estable del identificador (el mismo identificador da siempre el mismo hash).</summary>
    string Hash(string token);
}
