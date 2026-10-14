using Npgsql;

namespace Frontenis.Infrastructure.Persistence;

/// <summary>
/// Normaliza la cadena de conexión de PostgreSQL. Acepta tanto el formato de Npgsql
/// (<c>Host=...;Database=...</c>) como la URL que dan Neon y otros proveedores
/// (<c>postgresql://usuario:clave@host/bd?sslmode=require</c>).
/// </summary>
public static class PostgresConnectionString
{
    /// <summary>Segundos mínimos de espera al conectar: Neon puede tardar unos segundos en reanudar el cómputo.</summary>
    private const int MinConnectTimeoutSeconds = 30;

    /// <summary>Segundos que una conexión libre permanece abierta en el pool antes de cerrarse.</summary>
    private const int IdleConnectionSeconds = 30;

    /// <summary>Devuelve una cadena de conexión de Npgsql lista para usar.</summary>
    public static string Build(string? raw)
    {
        raw = raw?.Trim() ?? string.Empty;
        var builder = IsUrl(raw) ? FromUrl(raw) : new NpgsqlConnectionStringBuilder(raw);
        if (builder.Timeout < MinConnectTimeoutSeconds) builder.Timeout = MinConnectTimeoutSeconds;
        // Npgsql conserva las conexiones libres 300 s por defecto; con Neon eso puede retrasar la suspensión
        // del cómputo. Las cerramos antes (salvo que la cadena indique otro valor).
        if (!raw.Contains("Idle Lifetime", StringComparison.OrdinalIgnoreCase)) builder.ConnectionIdleLifetime = IdleConnectionSeconds;
        return builder.ConnectionString;
    }

    private static bool IsUrl(string value) =>
        value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
        value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase);

    private static NpgsqlConnectionStringBuilder FromUrl(string url)
    {
        var uri = new Uri(url);
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort || uri.Port <= 0 ? 5432 : uri.Port,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
            SslMode = SslMode.Require,
        };

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            var parts = uri.UserInfo.Split(':', 2);
            builder.Username = Uri.UnescapeDataString(parts[0]);
            if (parts.Length > 1) builder.Password = Uri.UnescapeDataString(parts[1]);
        }

        foreach (var pair in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = pair.Split('=', 2);
            if (kv.Length == 2 && kv[0].Equals("sslmode", StringComparison.OrdinalIgnoreCase))
                builder.SslMode = ParseSslMode(Uri.UnescapeDataString(kv[1]));
            // Otros parámetros de la URL (channel_binding, options…) se ignoran a propósito.
        }
        return builder;
    }

    private static SslMode ParseSslMode(string value) => value.ToLowerInvariant() switch
    {
        "disable" => SslMode.Disable,
        "allow" or "prefer" => SslMode.Prefer,
        "verify-ca" => SslMode.VerifyCA,
        "verify-full" => SslMode.VerifyFull,
        _ => SslMode.Require,
    };
}
