namespace Frontenis.Api.Auditing;

/// <summary>Limpia los textos que llegan de fuera antes de escribirlos en el registro, para que nadie pueda falsear líneas.</summary>
public static class AuditText
{
    /// <summary>Quita saltos de línea y otros caracteres de control, y recorta a <paramref name="max"/> caracteres.</summary>
    public static string Clean(string? text, int max = 300)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        var chars = text.Select(c => char.IsControl(c) ? ' ' : c).ToArray();
        var clean = new string(chars).Trim();
        return clean.Length <= max ? clean : clean[..max];
    }

    /// <summary>Solo se registran como pantallas rutas internas y cortas de la web (empiezan por «/»).</summary>
    public static bool IsValidPage(string? path) =>
        !string.IsNullOrEmpty(path) && path.Length <= 200 && path[0] == '/' && !path.StartsWith("//") && !path.Any(char.IsControl);
}
