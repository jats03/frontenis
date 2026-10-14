namespace Frontenis.Api.Middleware;

/// <summary>Cabeceras de seguridad en todas las respuestas; las de autenticación nunca se cachean.</summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly bool _isDevelopment;

    public SecurityHeadersMiddleware(RequestDelegate next, IHostEnvironment environment)
    {
        _next = next;
        _isDevelopment = environment.IsDevelopment();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "no-referrer";
        if (!_isDevelopment)
            headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'"; // (Swagger necesita scripts en desarrollo)
        if (context.Request.Path.StartsWithSegments("/api/auth")) headers["Cache-Control"] = "no-store";
        await _next(context);
    }
}
