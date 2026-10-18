namespace Frontenis.Application.Dtos;

/// <summary>Pantalla de la web que acaba de abrir alguien (ruta interna, por ejemplo «/reservas»).</summary>
public sealed record PageViewRequest(string Path);
