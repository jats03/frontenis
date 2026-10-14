namespace Frontenis.Application.Security;

/// <summary>Requisitos mínimos de contraseña de administrador.</summary>
public static class PasswordPolicy
{
    public const int MinLength = 12;
    public const int MaxLength = 128;

    private static readonly HashSet<string> Common = new(StringComparer.OrdinalIgnoreCase)
    {
        "password1234", "contraseña123", "123456789012", "admin1234567", "qwertyuiop12", "frontenis1234", "administrador",
    };

    /// <summary>Devuelve el motivo del rechazo, o null si la contraseña es aceptable.</summary>
    public static string? Validate(string? password, string username)
    {
        if (string.IsNullOrEmpty(password) || password.Length < MinLength)
            return $"La contraseña debe tener al menos {MinLength} caracteres.";
        if (password.Length > MaxLength) return $"La contraseña no puede superar los {MaxLength} caracteres.";
        if (password.Distinct().Count() < 5) return "La contraseña es demasiado repetitiva.";
        if (!string.IsNullOrWhiteSpace(username) && password.Contains(username, StringComparison.OrdinalIgnoreCase))
            return "La contraseña no puede contener el nombre de usuario.";
        if (Common.Contains(password)) return "Esa contraseña es demasiado común.";
        var kinds = new[] { password.Any(char.IsLower), password.Any(char.IsUpper), password.Any(char.IsDigit), password.Any(c => !char.IsLetterOrDigit(c)) };
        if (kinds.Count(k => k) < 2 && password.Length < 16)
            return "Combina mayúsculas, minúsculas, números o símbolos (o usa una frase de 16 caracteres o más).";
        return null;
    }
}
