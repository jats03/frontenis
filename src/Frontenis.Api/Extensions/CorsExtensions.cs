namespace Frontenis.Api.Extensions;

public static class CorsExtensions
{
    /// <summary>CORS limitado a los orígenes de la sección <c>Cors:Origins</c>.</summary>
    public static IServiceCollection AddFrontenisCors(this IServiceCollection services, IConfiguration config)
    {
        var origins = config.GetSection("Cors:Origins").Get<string[]>() ?? Array.Empty<string>();
        return services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod()));
    }
}
