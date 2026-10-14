using System.Threading.RateLimiting;
using Frontenis.Api.RateLimiting;

namespace Frontenis.Api.Extensions;

/// <summary>Registra las políticas de limitación de peticiones.</summary>
public static class RateLimitingExtensions
{
    /// <summary>
    /// Límites de peticiones por IP: general (frena abusos), estricto para login / cambio de contraseña
    /// (frena la fuerza bruta) y bajo para las altas públicas (frena el spam).
    /// </summary>
    public static IServiceCollection AddRateLimiting(this IServiceCollection services) =>
        services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(c =>
                FixedWindow(ClientKey(c), permits: 300));
            o.AddPolicy(RateLimitPolicies.PublicWrite, c =>
                FixedWindow($"{RateLimitPolicies.PublicWrite}:{ClientKey(c)}", permits: 20));
            o.AddPolicy(RateLimitPolicies.Login, c =>
                FixedWindow($"{RateLimitPolicies.Login}:{ClientKey(c)}", permits: 8));
        });

    private static string ClientKey(HttpContext c) => c.Connection.RemoteIpAddress?.ToString() ?? "desconocida";

    private static RateLimitPartition<string> FixedWindow(string key, int permits) =>
        RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        { PermitLimit = permits, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 });
}
