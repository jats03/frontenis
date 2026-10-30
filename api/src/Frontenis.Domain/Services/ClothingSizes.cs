using Frontenis.Domain.Exceptions;

namespace Frontenis.Domain.Services;

/// <summary>Tallas de ropa admitidas (camiseta y pantalón): de adulto (XS…3XL) y de niño (6…16).</summary>
public static class ClothingSizes
{
    public static readonly IReadOnlyList<string> Allowed =
        new[] { "6", "8", "10", "12", "14", "16", "XS", "S", "M", "L", "XL", "XXL", "3XL" };

    /// <summary>Longitud máxima de una talla guardada.</summary>
    public const int MaxLength = 4;

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
