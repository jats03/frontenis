using System.Text.Json;
using Frontenis.Api.Middleware;
using Frontenis.Application.Exceptions;
using Frontenis.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using Xunit;

namespace Frontenis.Tests;

public class ExceptionHandlingMiddlewareTests
{
    private sealed class CapturingLogger : ILogger<ExceptionHandlingMiddleware>
    {
        public readonly List<(LogLevel Level, string Message, Exception? Exception)> Entries = new();
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) => Entries.Add((logLevel, formatter(state, exception), exception));
    }

    private static DefaultHttpContext NewContext(string path = "/api/bookings")
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = path;
        context.TraceIdentifier = "trace-123";
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<(DefaultHttpContext Context, CapturingLogger Logger)> RunAsync(Exception ex)
    {
        var logger = new CapturingLogger();
        var middleware = new ExceptionHandlingMiddleware(_ => throw ex, logger);
        var context = NewContext();
        await middleware.InvokeAsync(context);
        return (context, logger);
    }

    private static JsonElement ReadBody(HttpContext context)
    {
        context.Response.Body.Position = 0;
        return JsonDocument.Parse(context.Response.Body).RootElement;
    }

    private static DbUpdateException UniqueViolation() =>
        new("Error al guardar", new PostgresException("duplicate key value violates unique constraint", "ERROR", "ERROR", "23505",
            tableName: "SlotAttendances", constraintName: "IX_SlotAttendances_SlotId_PlayerId"));

    [Fact]
    public async Task Two_identical_simultaneous_requests_give_200_and_409_never_500()
    {
        var logger = new CapturingLogger();
        var calls = 0;
        var middleware = new ExceptionHandlingMiddleware(ctx =>
        {
            if (Interlocked.Increment(ref calls) == 1) { ctx.Response.StatusCode = 200; return Task.CompletedTask; }
            throw UniqueViolation();
        }, logger);

        var first = NewContext();
        var second = NewContext();
        await Task.WhenAll(middleware.InvokeAsync(first), middleware.InvokeAsync(second));

        Assert.Equal(StatusCodes.Status200OK, first.Response.StatusCode);
        Assert.Equal(StatusCodes.Status409Conflict, second.Response.StatusCode);
        Assert.Equal("Conflicto", ReadBody(second).GetProperty("title").GetString());
        Assert.DoesNotContain(logger.Entries, e => e.Level >= LogLevel.Error);
    }

    [Fact]
    public async Task Unique_violation_logs_a_warning_with_route_and_conflict_type_without_stack_trace()
    {
        var (context, logger) = await RunAsync(UniqueViolation());

        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
        var warning = Assert.Single(logger.Entries, e => e.Level == LogLevel.Warning);
        Assert.Null(warning.Exception);
        Assert.Contains("/api/bookings", warning.Message);
        Assert.Contains("SlotAttendances", warning.Message);
        Assert.DoesNotContain(logger.Entries, e => e.Level >= LogLevel.Error);
    }

    [Fact]
    public async Task Other_database_errors_are_still_500()
    {
        var ex = new DbUpdateException("Error", new PostgresException("fk", "ERROR", "ERROR", "23503"));
        var (context, logger) = await RunAsync(ex);

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Error);
    }

    [Fact]
    public async Task ConflictException_maps_to_409_with_warning()
    {
        var (context, logger) = await RunAsync(new ConflictException("La partida ya tiene resultado.", "resultado ya enviado"));

        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
        var body = ReadBody(context);
        Assert.Equal("Conflicto", body.GetProperty("title").GetString());
        Assert.Equal("La partida ya tiene resultado.", body.GetProperty("detail").GetString());
        var warning = Assert.Single(logger.Entries, e => e.Level == LogLevel.Warning);
        Assert.Contains("resultado ya enviado", warning.Message);
    }

    [Fact]
    public async Task Client_abort_is_not_logged_as_error_and_has_no_body()
    {
        var logger = new CapturingLogger();
        using var cts = new CancellationTokenSource();
        var middleware = new ExceptionHandlingMiddleware(ctx =>
        {
            cts.Cancel();
            ctx.RequestAborted.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }, logger);
        var context = NewContext();
        context.RequestAborted = cts.Token;

        await middleware.InvokeAsync(context);

        Assert.Equal(ExceptionHandlingMiddleware.ClientClosedRequest, context.Response.StatusCode);
        Assert.Equal(0, context.Response.Body.Length);
        Assert.DoesNotContain(logger.Entries, e => e.Level >= LogLevel.Warning);
    }

    [Fact]
    public async Task Cancellation_not_caused_by_the_client_is_a_real_failure()
    {
        var (context, logger) = await RunAsync(new OperationCanceledException());

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Error);
    }

    public static TheoryData<Exception, int> ControlledErrors => new()
    {
        { new DomainException("Regla rota"), StatusCodes.Status400BadRequest },
        { new NotFoundException("No existe"), StatusCodes.Status404NotFound },
        { new UnauthorizedAppException("Sin sesión"), StatusCodes.Status401Unauthorized },
        { new TooManyAttemptsException(TimeSpan.FromSeconds(90)), StatusCodes.Status429TooManyRequests },
        { new ConflictException("Ya existe"), StatusCodes.Status409Conflict },
        { new InvalidOperationException("boom"), StatusCodes.Status500InternalServerError },
    };

    [Theory]
    [MemberData(nameof(ControlledErrors))]
    public async Task Every_error_is_problem_json_with_trace_id(Exception ex, int expectedStatus)
    {
        var (context, _) = await RunAsync(ex);

        Assert.Equal(expectedStatus, context.Response.StatusCode);
        Assert.StartsWith("application/problem+json", context.Response.ContentType);
        var body = ReadBody(context);
        Assert.Equal(expectedStatus, body.GetProperty("status").GetInt32());
        Assert.Equal("trace-123", body.GetProperty("traceId").GetString());
    }

    [Fact]
    public async Task Too_many_attempts_keeps_retry_after_header()
    {
        var (context, _) = await RunAsync(new TooManyAttemptsException(TimeSpan.FromSeconds(90)));

        Assert.Equal("90", context.Response.Headers.RetryAfter.ToString());
    }

    [Fact]
    public async Task Unexpected_error_hides_details_and_logs_error()
    {
        var (context, logger) = await RunAsync(new InvalidOperationException("secreto interno"));

        Assert.Equal("Se produjo un error inesperado.", ReadBody(context).GetProperty("detail").GetString());
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Error && e.Exception is InvalidOperationException);
    }
}
