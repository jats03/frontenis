namespace Frontenis.Infrastructure.Persistence;

/// <summary>
/// Scripts idempotentes (PostgreSQL) que ponen al día una BD ya existente: <c>EnsureCreated</c> crea las tablas
/// de una BD nueva pero no modifica las de una existente. El orden importa y cada script debe poder repetirse.
/// </summary>
/// <remarks>
/// La BD de PostgreSQL nace con el esquema completo (<c>sql/schema.sql</c> o <c>EnsureCreated</c>), por lo que
/// aquí solo se añaden los cambios posteriores al traspaso desde SQL Server. Ejemplo:
/// <c>ALTER TABLE "Players" ADD COLUMN IF NOT EXISTS "Nick" varchar(50) NULL;</c>
/// </remarks>
internal static class SchemaScripts
{
    public static IReadOnlyList<string> All { get; } = new[]
    {
        // Los entrenamientos ya no indican a cuántos tantos se juega.
        "ALTER TABLE \"TrainingMatches\" DROP COLUMN IF EXISTS \"TargetPoints\";",
    };
}
