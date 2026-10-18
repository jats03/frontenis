using System.Diagnostics;
using Frontenis.Application.Interfaces.Repositories;
using Frontenis.Api.Auditing.Interfaces;
using Frontenis.Domain.Entities;

namespace Frontenis.Api.Auditing;

/// <summary>
/// Registra TODA petición a la API en el log del servidor (quién, método, ruta, resultado y duración) y,
/// si modifica datos (POST, PUT o DELETE), también la guarda en la tabla <c>AuditLogs</c>.
/// </summary>
public sealed class RequestAuditMiddleware
{
    /// <summary>Aviso de pantalla de la web: va a su propio controlador y no se duplica aquí.</summary>
    public const string PageViewPath = "/api/audit/page";

    private static readonly HashSet<string> Writes = new(StringComparer.OrdinalIgnoreCase) { "POST", "PUT", "DELETE" };

    private readonly RequestDelegate _next;
    private readonly ILogger<RequestAuditMiddleware> _logger;

    public RequestAuditMiddleware(RequestDelegate next, ILogger<RequestAuditMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var request = context.Request;
        // Ni la comprobación de disponibilidad ni las peticiones previas de CORS son actividad de nadie.
        if (HttpMethods.IsOptions(request.Method) || request.Path.StartsWithSegments("/health")
            || request.Path.StartsWithSegments(PageViewPath))
        {
            await _next(context);
            return;
        }

        var clock = Stopwatch.StartNew();
        try { await _next(context); }
        finally
        {
            clock.Stop();
            await RecordAsync(context, (int)clock.ElapsedMilliseconds);
        }
    }

    private async Task RecordAsync(HttpContext context, int ms)
    {
        try
        {
            var request = context.Request;
            var actor = await context.RequestServices.GetRequiredService<IActorResolver>().ResolveAsync(context);
            var path = AuditText.Clean(request.Path.Value + request.QueryString.Value, AuditEntry.MaxPath);
            var status = context.Response.StatusCode;
            _logger.LogInformation("AUDIT {Kind} {Actor} {Method} {Path} -> {Status} ({Ms} ms)",
                actor.Kind, actor.Name, request.Method, path, status, ms);

            if (!Writes.Contains(request.Method)) return;
            // Alcance propio: la BD de la petición pudo quedar a medias si algo falló.
            using var scope = context.RequestServices.GetRequiredService<IServiceScopeFactory>().CreateScope();
            var entry = AuditEntry.Create(DateTime.UtcNow, actor.Kind, actor.Name, actor.PlayerId, request.Method, path, status, ms);
            await scope.ServiceProvider.GetRequiredService<IAuditRepository>().AddAsync(entry);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo guardar el registro de actividad.");
        }
    }
}
