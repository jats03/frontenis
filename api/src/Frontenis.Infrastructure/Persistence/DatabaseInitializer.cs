namespace Frontenis.Infrastructure.Persistence;

/// <summary>Deja la base de datos lista para usar: esquema al día y administrador sembrado.</summary>
public sealed class DatabaseInitializer
{
    private readonly SchemaUpdater _schema;
    private readonly AdminUserSeeder _admin;

    public DatabaseInitializer(SchemaUpdater schema, AdminUserSeeder admin)
    {
        _schema = schema;
        _admin = admin;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await _schema.UpdateAsync(ct);
        await _admin.SeedAsync(ct);
    }
}
