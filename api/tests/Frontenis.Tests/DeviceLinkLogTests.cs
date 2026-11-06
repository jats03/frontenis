using System.Net;
using Frontenis.Api.Auditing.Interfaces;
using Frontenis.Api.Controllers;
using Frontenis.Application.Dtos;
using Frontenis.Application.Interfaces.Services;
using Frontenis.Application.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Frontenis.Tests;

/// <summary>Al identificarse un jugador se escribe en el log quién es y desde qué IP (nada se guarda en la base de datos).</summary>
public class DeviceLinkLogTests
{
    private sealed class FakeDevices : IDeviceLinkService
    {
        public Task<DeviceMeDto?> GetMeAsync(string? deviceToken, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<PlayerRefDto>> ListAvailablePlayersAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<DevicePlayerDto>> ListSelectablePlayersAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public Task<DeviceMeDto> LinkAsync(string? deviceToken, int playerId, bool replace = false, CancellationToken ct = default) =>
            Task.FromResult(new DeviceMeDto(new PlayerRefDto(playerId, "Ana"), []));
        public Task UnlinkAsync(string? deviceToken, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<DeviceMeDto> AddDelegateAsync(string? deviceToken, int playerId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task RemoveDelegateAsync(string? deviceToken, int playerId, CancellationToken ct = default) => throw new NotSupportedException();
    }

    private sealed class FakeActors : IActorResolver
    {
        public Task<RequestActor> ResolveAsync(HttpContext context, bool lookup, CancellationToken ct = default) => throw new NotSupportedException();
        public void Remember(HttpContext context, int playerId, string playerName) { }
        public void Forget(HttpContext context) { }
    }

    private sealed class CapturingLogger : ILogger<DeviceController>
    {
        public List<(LogLevel Level, string Message)> Lines { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Lines.Add((logLevel, formatter(state, exception)));
    }

    private static (DeviceController Controller, CapturingLogger Log) Build(IPAddress? ip)
    {
        var log = new CapturingLogger();
        var controller = new DeviceController(new FakeDevices(), new FakeActors(), log)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        controller.HttpContext.Connection.RemoteIpAddress = ip;
        return (controller, log);
    }

    [Fact]
    public async Task Al_identificarse_se_registra_jugador_e_ip()
    {
        var (controller, log) = Build(IPAddress.Parse("203.0.113.7"));

        var result = await controller.Link("token", new LinkDeviceRequest(5, false), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
        var line = Assert.Single(log.Lines);
        Assert.Equal(LogLevel.Information, line.Level);
        Assert.Equal("Dispositivo identificado como Ana (id 5) desde la IP 203.0.113.7", line.Message);
    }

    [Fact]
    public async Task Sin_ip_conocida_se_registra_desconocida()
    {
        var (controller, log) = Build(null);

        await controller.Link("token", new LinkDeviceRequest(5, false), CancellationToken.None);

        Assert.Contains("desde la IP desconocida", Assert.Single(log.Lines).Message);
    }
}
