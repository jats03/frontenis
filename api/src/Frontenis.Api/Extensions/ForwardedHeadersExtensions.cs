using Microsoft.AspNetCore.HttpOverrides;

namespace Frontenis.Api.Extensions;

/// <summary>Respeta las cabeceras del proxy para obtener la IP real del cliente.</summary>
public static class ForwardedHeadersExtensions
{
    /// <summary>
    /// Render pone un proxy delante: sin esto, todas las peticiones parecerían venir de la misma IP.
    /// ForwardLimit = 1 usa solo la IP que añadió el proxy de Render, no la que el cliente pueda falsear.
    /// </summary>
    public static IServiceCollection AddProxyForwarding(this IServiceCollection services) =>
        services.Configure<ForwardedHeadersOptions>(o =>
        {
            o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            o.ForwardLimit = 1;
            o.KnownNetworks.Clear();
            o.KnownProxies.Clear();
        });
}
