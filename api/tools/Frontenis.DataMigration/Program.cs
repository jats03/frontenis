using Frontenis.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Npgsql;

// Copia todos los datos de SQL Server (Azure) a PostgreSQL (Neon). Se puede ejecutar tantas veces como haga falta:
// cada ejecución VACÍA las tablas de destino y las vuelve a llenar (conserva los identificadores).
//
//   SOURCE_SQLSERVER  cadena de conexión de Azure SQL
//   TARGET_POSTGRES   cadena de conexión o URL de Neon
//   --yes             confirma que se borra y rellena el destino (sin él solo se muestran los recuentos)

var sourceCs = Environment.GetEnvironmentVariable("SOURCE_SQLSERVER");
var targetCs = Environment.GetEnvironmentVariable("TARGET_POSTGRES");
if (string.IsNullOrWhiteSpace(sourceCs) || string.IsNullOrWhiteSpace(targetCs))
{
    Console.Error.WriteLine("Define las variables de entorno SOURCE_SQLSERVER y TARGET_POSTGRES.");
    return 2;
}
var confirmed = args.Contains("--yes");

// Orden que respeta las claves foráneas.
string[] tables =
{
    "Players", "Users", "TrainingSessions", "TrainingMatches", "Pairs",
    "Competitions", "Groups", "CompetitionEntries", "Matches",
};

await using var source = new SqlConnection(sourceCs);
await source.OpenAsync();
await using var target = new NpgsqlConnection(PostgresConnectionString.Build(targetCs));
await target.OpenAsync();

// 1) Esquema de destino
var schemaPath = FindSchema();
await using (var create = new NpgsqlCommand(await File.ReadAllTextAsync(schemaPath), target))
    await create.ExecuteNonQueryAsync();
Console.WriteLine($"Esquema de PostgreSQL listo ({schemaPath}).");

// 2) Recuentos de origen
var sourceCounts = new Dictionary<string, long>();
foreach (var t in tables) sourceCounts[t] = await CountAsync(source, $"SELECT COUNT(*) FROM [{t}]");
Console.WriteLine("Filas en origen:   " + string.Join(", ", tables.Select(t => $"{t}={sourceCounts[t]}")));

if (!confirmed)
{
    Console.WriteLine("Nada copiado. Añade --yes para VACIAR el destino y copiar los datos.");
    return 0;
}

// 3) Vaciar y copiar en una sola transacción: si algo falla, el destino queda como estaba.
await using var tx = await target.BeginTransactionAsync();
var truncate = "TRUNCATE TABLE " + string.Join(", ", tables.Select(t => $"\"{t}\"")) + " RESTART IDENTITY CASCADE";
await using (var cmd = new NpgsqlCommand(truncate, target, tx)) await cmd.ExecuteNonQueryAsync();

foreach (var table in tables)
{
    var copied = await CopyTableAsync(source, target, tx, table);
    Console.WriteLine($"  {table}: {copied} fila(s)");
}

// 4) Que los próximos INSERT de la API continúen tras el último identificador copiado.
foreach (var table in tables)
{
    var sql = $"SELECT setval(pg_get_serial_sequence('\"{table}\"', 'Id'), (SELECT COALESCE(MAX(\"Id\"), 0) + 1 FROM \"{table}\"), false)";
    await using var cmd = new NpgsqlCommand(sql, target, tx);
    await cmd.ExecuteScalarAsync();
}

// 5) Verificación antes de confirmar
var ok = true;
foreach (var t in tables)
{
    await using var cmd = new NpgsqlCommand($"SELECT COUNT(*) FROM \"{t}\"", target, tx);
    var n = Convert.ToInt64(await cmd.ExecuteScalarAsync());
    if (n != sourceCounts[t]) { ok = false; Console.Error.WriteLine($"DIFERENCIA en {t}: origen {sourceCounts[t]}, destino {n}"); }
}
if (!ok)
{
    await tx.RollbackAsync();
    Console.Error.WriteLine("Traspaso cancelado: los recuentos no coinciden. El destino no se ha modificado.");
    return 1;
}
await tx.CommitAsync();
Console.WriteLine("Traspaso completado y verificado.");
return 0;

static async Task<long> CountAsync(SqlConnection c, string sql)
{
    await using var cmd = new SqlCommand(sql, c);
    return Convert.ToInt64(await cmd.ExecuteScalarAsync());
}

static async Task<int> CopyTableAsync(SqlConnection source, NpgsqlConnection target, NpgsqlTransaction tx, string table)
{
    await using var read = new SqlCommand($"SELECT * FROM [{table}]", source);
    await using var reader = await read.ExecuteReaderAsync();
    var columns = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToArray();
    var idIndex = Array.IndexOf(columns, "Id");
    // Matches apunta a otra fila de la misma tabla: se inserta sin NextMatchId y se enlaza al final.
    var nextIndex = table == "Matches" ? Array.IndexOf(columns, "NextMatchId") : -1;
    var links = new List<(int Id, int Next)>();

    var insert = $"INSERT INTO \"{table}\" ({string.Join(", ", columns.Select(c => $"\"{c}\""))}) " +
                 $"VALUES ({string.Join(", ", columns.Select((_, i) => $"${i + 1}"))})";
    var count = 0;
    var batch = new NpgsqlBatch(target, tx);
    while (await reader.ReadAsync())
    {
        var values = new object[columns.Length];
        reader.GetValues(values);
        if (nextIndex >= 0 && values[nextIndex] is not DBNull)
        {
            links.Add((Convert.ToInt32(values[idIndex]), Convert.ToInt32(values[nextIndex])));
            values[nextIndex] = DBNull.Value;
        }
        var command = new NpgsqlBatchCommand(insert);
        foreach (var v in values) command.Parameters.Add(new NpgsqlParameter { Value = ToPostgres(v) });
        batch.BatchCommands.Add(command);
        count++;
        if (batch.BatchCommands.Count >= 200) { await batch.ExecuteNonQueryAsync(); batch = new NpgsqlBatch(target, tx); }
    }
    if (batch.BatchCommands.Count > 0) await batch.ExecuteNonQueryAsync();

    foreach (var (id, next) in links)
    {
        await using var update = new NpgsqlCommand("UPDATE \"Matches\" SET \"NextMatchId\" = $1 WHERE \"Id\" = $2", target, tx);
        update.Parameters.Add(new NpgsqlParameter { Value = next });
        update.Parameters.Add(new NpgsqlParameter { Value = id });
        await update.ExecuteNonQueryAsync();
    }
    return count;
}

// tinyint (byte) -> smallint; datetime2 -> UTC (Npgsql no admite otro Kind en timestamptz).
static object ToPostgres(object value) => value switch
{
    DBNull => DBNull.Value,
    byte b => (short)b,
    DateTime d => DateTime.SpecifyKind(d, DateTimeKind.Utc),
    _ => value,
};

static string FindSchema()
{
    for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
    {
        var candidate = Path.Combine(dir.FullName, "sql", "schema.sql");
        if (File.Exists(candidate)) return candidate;
    }
    throw new FileNotFoundException("No se encuentra sql/schema.sql. Ejecuta la herramienta desde el repositorio.");
}
