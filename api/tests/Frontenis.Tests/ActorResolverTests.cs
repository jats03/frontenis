using Frontenis.Api.Auditing;
using Frontenis.Application.Interfaces.Services;
using Frontenis.Application.Models;
using Frontenis.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace Frontenis.Tests;

/// <summary>Quién sale en el registro de actividad: las lecturas no consultan la BD, pero pueden recordar lo ya averiguado.</summary>
public class ActorResolverTests
{
    private const string Phone = "phoneAAAA1";

    /// <summary>Falla si se consulta la BD: así se demuestra que una lectura no la toca.</summary>
    private sealed class NoDatabase : IDeviceAccessResolver
    {
        public Task<DeviceAccess?> FindAsync(string? deviceToken, CancellationToken ct = default) => throw new InvalidOperationException("consulta a la BD");
        public Task<DeviceAccess> ResolveAsync(string? deviceToken, CancellationToken ct = default) => throw new InvalidOperationException("consulta a la BD");
    }

    private static ActorResolver Build(IDeviceAccessResolver? devices = null, IMemoryCache? cache = null) =>
        new(devices ?? new NoDatabase(), new FakePlayers(), cache ?? new MemoryCache(new MemoryCacheOptions()));

    private static HttpContext Context(string? token = Phone)
    {
        var ctx = new DefaultHttpContext();
        if (token is not null) ctx.Request.Headers[ActorResolver.DeviceHeader] = token;
        return ctx;
    }

    [Fact]
    public async Task A_read_from_a_device_nobody_has_identified_is_a_visitor_and_never_queries_the_database()
    {
        var actor = await Build().ResolveAsync(Context(), lookup: false);
        Assert.Equal(RequestActor.Visitor, actor);
    }

    [Fact]
    public async Task After_remembering_the_player_reads_show_the_name_without_querying_the_database()
    {
        var resolver = Build();
        var ctx = Context();

        resolver.Remember(ctx, 7, "Ana");
        var actor = await resolver.ResolveAsync(ctx, lookup: false);

        Assert.Equal((ActorKind.Player, "Ana", (int?)7), (actor.Kind, actor.Name, actor.PlayerId));
    }

    [Fact]
    public async Task What_is_remembered_belongs_to_that_device_only()
    {
        var resolver = Build();
        resolver.Remember(Context(Phone), 7, "Ana");

        Assert.Equal(RequestActor.Visitor, await resolver.ResolveAsync(Context("phoneBBBB2"), lookup: false));
        Assert.Equal(RequestActor.Visitor, await resolver.ResolveAsync(Context(token: null), lookup: false));
    }

    [Fact]
    public async Task Forgetting_a_device_makes_it_a_visitor_again()
    {
        var resolver = Build();
        var ctx = Context();
        resolver.Remember(ctx, 7, "Ana");

        resolver.Forget(ctx);

        Assert.Equal(RequestActor.Visitor, await resolver.ResolveAsync(ctx, lookup: false));
    }

    [Fact]
    public void Remembering_without_a_device_identifier_does_nothing()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        Build(cache: cache).Remember(Context(token: null), 7, "Ana");
        Assert.Equal(0, cache.Count);
    }

    [Fact]
    public async Task The_remembered_name_is_cleaned_like_any_other_logged_text()
    {
        var resolver = Build();
        var ctx = Context();

        resolver.Remember(ctx, 7, "Ana\r\nAUDIT falso");

        var actor = await resolver.ResolveAsync(ctx, lookup: false);
        Assert.DoesNotContain('\n', actor.Name);
        Assert.DoesNotContain('\r', actor.Name);
    }
}
