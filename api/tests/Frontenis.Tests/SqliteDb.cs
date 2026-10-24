using Frontenis.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Frontenis.Tests;

/// <summary>
/// Base de datos SQLite en memoria con el modelo real de EF Core, para probar los repositorios sin Postgres.
/// Limitaciones: no es PostgreSQL (colación, tipos <c>timestamptz</c>/<c>date</c>, <c>SchemaScripts</c> y reintentos
/// quedan fuera). Si una prueba falla se registra el SQL generado en <see cref="Sql"/>.
/// </summary>
public sealed class SqliteDb : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:;Foreign Keys=True");
    private readonly DbContextOptions<FrontenisDbContext> _options;

    /// <summary>Sentencias SQL ejecutadas, para diagnosticar un fallo.</summary>
    public List<string> Sql { get; } = new();

    public SqliteDb()
    {
        _connection.Open();
        // El modelo usa char_length (PostgreSQL) en una restricción CHECK; SQLite solo conoce length.
        _connection.CreateFunction("char_length", (string s) => s.Length);
        _options = new DbContextOptionsBuilder<FrontenisDbContext>()
            .UseSqlite(_connection)
            .LogTo(Sql.Add, Microsoft.Extensions.Logging.LogLevel.Information)
            .Options;
        using var db = NewContext();
        db.Database.EnsureCreated();
    }

    /// <summary>Un contexto nuevo sobre la misma base de datos (sin entidades en caché de otro contexto).</summary>
    public FrontenisDbContext NewContext() => new(_options);

    public void Dispose() => _connection.Dispose();

    /// <summary>Texto con el SQL generado, para añadirlo al mensaje de una aserción.</summary>
    public string SqlLog() => string.Join(Environment.NewLine, Sql);
}
