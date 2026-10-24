using Frontenis.Application.Exceptions;
using Frontenis.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Frontenis.Api.Middleware;

/// <summary>
/// Traduce excepciones a respuestas HTTP coherentes (ProblemDetails, <c>application/problem+json</c>, con <c>traceId</c>).
/// Conflictos (409) se registran como aviso; solo los fallos reales (500) como error; la cancelación del cliente, como nada grave.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>Código no estándar (nginx) para "el cliente cerró la petición".</summary>
    public const int ClientClosedRequest = StatusCodes.Status499ClientClosedRequest;
    public const string ProblemJson = "application/problem+json";
    /// <summary>SqlState de PostgreSQL para violación de índice único.</summary>
    private const string UniqueViolation = "23505";

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // El cliente abortó (p. ej. cerró la pestaña o repitió el toque): no es un fallo del servidor.
            _logger.LogInformation("Petición cancelada por el cliente: {Method} {Path}", context.Request.Method, context.Request.Path);
            if (!context.Response.HasStarted) context.Response.StatusCode = ClientClosedRequest;
        }
        catch (Exception ex)
        {
            var (status, title, detail) = Classify(ex);
            var path = context.Request.Path.Value;
            switch (status)
            {
                case StatusCodes.Status409Conflict:
                    // Sin la excepción: el aviso no lleva traza de pila.
                    _logger.LogWarning("Conflicto ({ConflictType}) en {Method} {Path}. traceId={TraceId}",
                        ConflictType(ex), context.Request.Method, path, context.TraceIdentifier);
                    break;
                case StatusCodes.Status500InternalServerError:
                    _logger.LogError(ex, "Error no controlado en {Method} {Path}. traceId={TraceId}",
                        context.Request.Method, path, context.TraceIdentifier);
                    break;
            }

            if (context.Response.HasStarted)
            {
                // Ya se envió parte de la respuesta: no se puede reescribir. Un fallo real corta la conexión.
                _logger.LogWarning("No se pudo escribir la respuesta de error {Status}: la respuesta ya había empezado", status);
                if (status == StatusCodes.Status500InternalServerError) throw;
                return;
            }

            context.Response.StatusCode = status;
            if (ex is TooManyAttemptsException tooMany)
                context.Response.Headers.RetryAfter = ((int)Math.Ceiling(tooMany.RetryAfter.TotalSeconds)).ToString();
            var problem = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = detail,
                Instance = path
            };
            problem.Extensions["traceId"] = context.TraceIdentifier;
            await context.Response.WriteAsJsonAsync(problem, options: null, contentType: ProblemJson);
        }
    }

    private static (int Status, string Title, string Detail) Classify(Exception ex) => ex switch
    {
        DomainException => (StatusCodes.Status400BadRequest, "Solicitud no válida", ex.Message),
        NotFoundException => (StatusCodes.Status404NotFound, "No encontrado", ex.Message),
        UnauthorizedAppException => (StatusCodes.Status401Unauthorized, "No autorizado", ex.Message),
        TooManyAttemptsException => (StatusCodes.Status429TooManyRequests, "Demasiados intentos", ex.Message),
        ConflictException => (StatusCodes.Status409Conflict, "Conflicto", ex.Message),
        DbUpdateException { InnerException: PostgresException { SqlState: UniqueViolation } } =>
            (StatusCodes.Status409Conflict, "Conflicto", "Esa operación ya se había registrado (posible envío duplicado)."),
        _ => (StatusCodes.Status500InternalServerError, "Error interno", "Se produjo un error inesperado.")
    };

    /// <summary>Descripción del conflicto para el log: tipo y, si es de BD, tabla e índice (nunca valores).</summary>
    private static string ConflictType(Exception ex) => ex switch
    {
        ConflictException c => c.ConflictType,
        DbUpdateException { InnerException: PostgresException pg } =>
            $"registro duplicado tabla={pg.TableName ?? "?"} indice={pg.ConstraintName ?? "?"}",
        _ => ex.GetType().Name
    };
}
