using System.Net;
using Frontenis.Api.Extensions;
using Frontenis.Api.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Frontenis.Tests;

public class IpAnonymizerTests
{
    [Theory]
    [InlineData("192.168.1.57", "192.168.1.0")]
    [InlineData("::ffff:10.20.30.40", "10.20.30.0")]
    [InlineData("2001:db8:abcd:1234:5678:9abc:def0:1234", "2001:db8:abcd::")]
    public void Hides_the_tail_of_the_address(string ip, string expected) =>
        Assert.Equal(expected, IpAnonymizer.Anonymize(IPAddress.Parse(ip)));

    [Fact]
    public void Unknown_address_is_reported_as_such() => Assert.Equal("desconocida", IpAnonymizer.Anonymize(null));
}

public class SuggestionRateLimitTests
{
    private sealed class CapturingProvider : ILoggerProvider
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = new();
        public ILogger CreateLogger(string categoryName) => new CapturingLogger(Entries);
        public void Dispose() { }
        private sealed class CapturingLogger(List<(LogLevel, string)> entries) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
                entries.Add((logLevel, formatter(state, exception)));
        }
    }

    private static async Task<(TestServer Server, CapturingProvider Logs)> StartAsync()
    {
        var logs = new CapturingProvider();
        var host = await new HostBuilder().ConfigureWebHost(web => web
            .UseTestServer()
            .ConfigureLogging(l => l.AddProvider(logs))
            .ConfigureServices(s => { s.AddRouting(); s.AddRateLimiting(); })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseRateLimiter();
                app.UseEndpoints(e => e.MapPost("/sugerencia", () => Results.Ok())
                    .RequireRateLimiting(RateLimitPolicies.Suggestions));
            })).StartAsync();
        return (host.GetTestServer(), logs);
    }

    [Fact]
    public async Task The_21st_call_in_a_minute_is_rejected_with_429_and_Retry_After()
    {
        var (server, logs) = await StartAsync();
        using var client = server.CreateClient();

        for (var i = 1; i <= 20; i++)
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/sugerencia", null)).StatusCode);
        var rejected = await client.PostAsync("/sugerencia", null);

        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.True(rejected.Headers.TryGetValues("Retry-After", out var retry));
        Assert.InRange(int.Parse(retry!.Single()), 1, 60);
        var warning = Assert.Single(logs.Entries, e => e.Level == LogLevel.Warning);
        Assert.Contains("/sugerencia", warning.Message);
    }
}
