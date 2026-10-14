using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Frontenis.Infrastructure.Persistence;

/// <summary>
/// Npgsql exige <c>DateTimeKind.Utc</c> al escribir en <c>timestamptz</c>. Guardamos siempre como UTC y
/// leemos con <c>Kind = Unspecified</c>, igual que hacía SQL Server, para no alterar el JSON que ve la web.
/// </summary>
public sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter()
        : base(v => DateTime.SpecifyKind(v, DateTimeKind.Utc), v => DateTime.SpecifyKind(v, DateTimeKind.Unspecified)) { }
}
