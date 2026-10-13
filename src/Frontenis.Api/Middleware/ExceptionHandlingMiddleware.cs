using Frontenis.Application.Services;
using Frontenis.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Frontenis.Api.Middleware;

/// <summary>Traduce excepciones de dominio a respuestas HTTP coherentes (ProblemDetails).</summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var (status, title) = ex switch
            {
                DomainException => (StatusCodes.Status400BadRequest, "Solicitud no válida"),
                NotFoundException => (StatusCodes.Status404NotFound, "No encontrado"),
                UnauthorizedAppException => (StatusCodes.Status401Unauthorized, "No autorizado"),
                TooManyAttemptsException => (StatusCodes.Status429TooManyRequests, "Demasiados intentos"),
                _ => (StatusCodes.Status500InternalServerError, "Error interno")
            };
            if (status == StatusCodes.Status500InternalServerError) _logger.LogError(ex, "Error no controlado");

            context.Response.StatusCode = status;
            if (ex is TooManyAttemptsException tooMany)
                context.Response.Headers.RetryAfter = ((int)Math.Ceiling(tooMany.RetryAfter.TotalSeconds)).ToString();
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = status == 500 ? "Se produjo un error inesperado." : ex.Message
            });
        }
    }
}
