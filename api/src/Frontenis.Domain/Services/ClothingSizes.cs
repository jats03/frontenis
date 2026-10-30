using Frontenis.Domain.Exceptions;

namespace Frontenis.Domain.Services;

/// <summary>
/// Tallas de ropa admitidas (camiseta y pantalón): de niño (6…16) y de adulto de la S a la 4XL, cada una de mujer (sufijo «M»,
/// por ejemplo «SM») o de hombre (sufijo «H», por ejemplo «SH»). Las tallas de adulto sin sexo que ya se habían guardado
/// (XS, S, M, L, XL, XXL, 3XL) se siguen aceptando, aunque ya no se ofrezcan.
/// </summary>
public static class ClothingSizes
{
    private static readonly string[] Children = { "6", "8", "10", "12", "14", "16" };
    /// <summary>Tallas de adulto que se ofrecen (sin el sexo).</summary>
    private static readonly string[] Adult = { "S", "M", "L", "XL", "XXL", "3XL", "4XL" };
    /// <summary>Tallas de adulto sin sexo guardadas antes de separar mujer y hombre.</summary>
    private static readonly string[] Legacy = { "XS", "S", "M", "L", "XL", "XXL", "3XL" };

    /// <summary>Sufijo de la talla de mujer.</summary>
    public const string Woman = "M";
    /// <summary>Sufijo de la talla de hombre.</summary>
    public const string Man = "H";

    /// <summary>Tallas que se ofrecen hoy: las de niño y las de adulto de mujer y de hombre.</summary>
    public static readonly IReadOnlyList<string> Offered =
        Children.Concat(Adult.SelectMany(s => new[] { s + Woman, s + Man })).ToArray();

    /// <summary>Todas las tallas válidas: las que se ofrecen y las antiguas sin sexo.</summary>
    public static readonly IReadOnlyList<string> Allowed = Children.Concat(Legacy).Concat(Offered.Skip(Children.Length)).ToArray();

    /// <summary>Longitud máxima de una talla guardada.</summary>
    public const int MaxLength = 4; // la más larga: «4XLM»

    /// <summary>Talla limpia (sin espacios y en mayúsculas) o <c>null</c> si no se indica ninguna.</summary>
    /// <exception cref="DomainException">La talla no está en la lista.</exception>
    public static string? Normalize(string? size)
    {
        var clean = size?.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(clean)) return null;
        if (!Allowed.Contains(clean)) throw new DomainException("Talla no válida.");
        return clean;
    }
}
