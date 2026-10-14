namespace Frontenis.Domain.Models;

/// <summary>Clasificación de un grupo (filas ya ordenadas).</summary>
public sealed record GroupStandings(int GroupId, string GroupName, IReadOnlyList<StandingRow> Rows);
