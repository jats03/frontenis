using System.Globalization;
using System.Text;
using Frontenis.Domain.Exceptions;

namespace Frontenis.Domain.Services;

/// <summary>
/// Reglas del nombre de un jugador: cómo se guarda (<see cref="Normalize"/>) y cuándo dos nombres son «el mismo»
/// (<see cref="Key"/>), para no tener dos jugadores que solo se diferencian en espacios, mayúsculas o tildes.
/// </summary>
public static class PlayerNameRules
{
    /// <summary>Longitud máxima del nombre (también la limita la BD con <c>CK_Players_NameLength</c>).</summary>
    public const int MaxLength = 40;

    /// <summary>
    /// Nombre limpio para guardar y mostrar: sin caracteres de control ni invisibles (espacio de ancho cero, marcas de
    /// dirección…), con cualquier tipo de espacio (tabulador, espacio duro, de ancho especial…) convertido en un espacio
    /// normal, sin espacios repetidos ni al principio o al final, y en forma Unicode compuesta (NFC).
    /// </summary>
    public static string Normalize(string? name)
    {
        if (string.IsNullOrEmpty(name)) return string.Empty;
        var sb = new StringBuilder(name.Length);
        var pendingSpace = false;
        foreach (var rune in name.Normalize(NormalizationForm.FormC).EnumerateRunes())
        {
            var category = Rune.GetUnicodeCategory(rune);
            if (Rune.IsWhiteSpace(rune) || category is UnicodeCategory.SpaceSeparator or UnicodeCategory.LineSeparator
                or UnicodeCategory.ParagraphSeparator)
            {
                pendingSpace = sb.Length > 0;
                continue;
            }
            if (category is UnicodeCategory.Control or UnicodeCategory.Format or UnicodeCategory.PrivateUse
                or UnicodeCategory.OtherNotAssigned or UnicodeCategory.Surrogate)
                continue;
            if (pendingSpace) { sb.Append(' '); pendingSpace = false; }
            sb.Append(rune.ToString());
        }
        return sb.ToString();
    }

    /// <summary>
    /// Clave para comparar nombres: ignora mayúsculas, tildes y diéresis, espacios, guiones, apóstrofos y cualquier otro
    /// signo; solo cuentan letras y números. «José  Alejandro», «jose alejandro» y «Jose-Alejandro» dan la misma clave.
    /// </summary>
    public static string Key(string? name)
    {
        var sb = new StringBuilder();
        // FormKD: además de separar las tildes, convierte «ª», «ﬁ», letras de ancho completo… en su letra normal.
        foreach (var ch in Normalize(name).Normalize(NormalizationForm.FormKD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsLetterOrDigit(ch)) sb.Append(char.ToLowerInvariant(ch));
        }
        return sb.ToString().Replace("ß", "ss");
    }

    /// <summary>Indica si dos nombres se consideran el mismo.</summary>
    public static bool Same(string? a, string? b)
    {
        var ka = Key(a);
        return ka.Length > 0 && ka == Key(b);
    }

    /// <summary>Devuelve el nombre limpio o lanza <see cref="DomainException"/> si no es válido.</summary>
    public static string Validate(string? name)
    {
        var clean = Normalize(name);
        if (clean.Length == 0) throw new DomainException("El nombre del jugador es obligatorio.");
        if (clean.Length > MaxLength) throw new DomainException($"El nombre no puede pasar de {MaxLength} caracteres.");
        if (Key(clean).Length == 0) throw new DomainException("El nombre debe contener letras o números.");
        return clean;
    }
}
