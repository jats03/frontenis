using System.Diagnostics;
using Frontenis.Application.Interfaces.Repositories;
using Frontenis.Application.Models;
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
    private const string DeviceLinkPath = "/api/device/link";

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

        // Vincular o desvincular un móvil: se anota también el dispositivo. Al desvincular, el jugador se averigua
        // antes de que se borre el vínculo.
        var deviceChange = IsDeviceLinkChange(request);
        RequestActor? before = null;
        if (deviceChange && HttpMethods.IsDelete(request.Method))
            before = await context.RequestServices.GetRequiredService<IActorResolver>().ResolveAsync(context, lookup: true);

        var clock = Stopwatch.StartNew();
        try { await _next(context); }
        finally
        {
            clock.Stop();
            await RecordAsync(context, (int)clock.ElapsedMilliseconds, deviceChange, before);
        }
    }

    private static bool IsDeviceLinkChange(HttpRequest request) =>
        request.Path.Equals(DeviceLinkPath, StringComparison.OrdinalIgnoreCase)
        && (HttpMethods.IsPost(request.Method) || HttpMethods.IsDelete(request.Method));

    private async Task RecordAsync(HttpContext context, int ms, bool deviceChange, RequestActor? before)
    {
        try
        {
            var request = context.Request;
            var isWrite = Writes.Contains(request.Method);
            // Las lecturas no consultan la BD para saber quién es: así no despiertan Neon cuando está suspendida.
            var resolver = context.RequestServices.GetRequiredService<IActorResolver>();
            if (deviceChange) resolver.Forget(context); // el vínculo acaba de cambiar: que no valga lo recordado
            var actor = before ?? await resolver.ResolveAsync(context, lookup: isWrite);
            var device = deviceChange ? AuditDevice.Describe(context) : null;
            var path = AuditText.Clean(request.Path.Value + request.QueryString.Value, AuditEntry.MaxPath);
            var status = context.Response.StatusCode;
            _logger.LogInformation("AUDIT {Kind} {Actor} {Method} {Path} -> {Status} ({Ms} ms){Device}",
                actor.Kind, actor.Name, request.Method, path, status, ms, device is null ? "" : " dispositivo=" + device);

            if (!isWrite) return;
            // Alcance propio: la BD de la petición pudo quedar a medias si algo falló.
            using var scope = context.RequestServices.GetRequiredService<IServiceScopeFactory>().CreateScope();
            var entry = AuditEntry.Create(DateTime.UtcNow, actor.Kind, actor.Name, actor.PlayerId, request.Method, path, status, ms, device);
            await scope.ServiceProvider.GetRequiredService<IAuditRepository>().AddAsync(entry);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo guardar el registro de actividad.");
        }
    }
}
