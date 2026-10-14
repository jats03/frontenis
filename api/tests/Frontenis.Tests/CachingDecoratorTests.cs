using Frontenis.Application.Caching;
using Frontenis.Application.Dtos;
using Frontenis.Application.Interfaces.Services;
using Frontenis.Application.Models;
using Xunit;

namespace Frontenis.Tests;

public class CachingDecoratorTests
{
    private sealed class FakeCompetitionQueries : ICompetitionQueries
    {
        public int ListCalls;
        public CancellationToken LastToken;

        public Task<IReadOnlyList<CompetitionSummaryDto>> ListAsync(CancellationToken ct = default)
        {
            ListCalls++;
            LastToken = ct;
            return Task.FromResult<IReadOnlyList<CompetitionSummaryDto>>(new List<CompetitionSummaryDto>());
        }

        public Task<CompetitionDetailDto> GetAsync(int id, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<GroupStandingsDto>> GetStandingsAsync(int id, CancellationToken ct = default) => throw new NotSupportedException();
    }

    private sealed class FakeCompetitionCommands : ICompetitionCommands
    {
        public int Deleted;
        public Task<CompetitionSummaryDto> CreateAsync(CreateCompetitionRequest request, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<CompetitionSummaryDto> RenameAsync(int id, RenameCompetitionRequest request, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<CompetitionSummaryDto> ChangeSetsPerMatchAsync(int id, SetsPerMatchRequest request, CancellationToken ct = default) => throw new NotSupportedException();
        public Task DeleteAsync(int id, CancellationToken ct = default) { Deleted++; return Task.CompletedTask; }
    }

    [Fact]
    public async Task Queries_are_served_from_cache_until_it_is_cleared()
    {
        var inner = new FakeCompetitionQueries();
        var cache = new ReadCache();
        var cached = new CachedCompetitionQueries(inner, cache);

        await cached.ListAsync();
        await cached.ListAsync();
        Assert.Equal(1, inner.ListCalls);

        cache.Clear();
        await cached.ListAsync();
        Assert.Equal(2, inner.ListCalls);
    }

    [Fact]
    public async Task Shared_result_is_not_tied_to_the_request_cancellation_token()
    {
        var inner = new FakeCompetitionQueries();
        var cached = new CachedCompetitionQueries(inner, new ReadCache());
        using var cts = new CancellationTokenSource();

        await cached.ListAsync(cts.Token);

        Assert.False(inner.LastToken.CanBeCanceled);
    }

    [Fact]
    public async Task Deleting_a_competition_clears_the_cache()
    {
        var inner = new FakeCompetitionQueries();
        var cache = new ReadCache();
        var queries = new CachedCompetitionQueries(inner, cache);
        var commands = new FakeCompetitionCommands();
        var invalidating = new CacheInvalidatingCompetitionCommands(commands, cache);

        await queries.ListAsync();
        await invalidating.DeleteAsync(1);
        await queries.ListAsync();

        Assert.Equal(1, commands.Deleted);
        Assert.Equal(2, inner.ListCalls);
    }

    [Fact]
    public void History_cache_keys_do_not_depend_on_filter_order()
    {
        var a = new HistoryFilter(new[] { 3, 1 }, new[] { 9, 2 });
        var b = new HistoryFilter(new[] { 1, 3 }, new[] { 2, 9 });
        Assert.Equal(CacheKeys.HistoryRanking(a), CacheKeys.HistoryRanking(b));
        Assert.NotEqual(CacheKeys.HistoryRanking(a), CacheKeys.HistoryRanking(HistoryFilter.All));
        Assert.NotEqual(CacheKeys.HistoryPlayer(1, a), CacheKeys.HistoryPlayer(2, a));
    }

    [Fact]
    public async Task NullReadCache_ejecuta_la_consulta_real_cada_vez()
    {
        var cache = new NullReadCache();
        var calls = 0;
        await cache.GetOrAddAsync("k", () => Task.FromResult(++calls));
        await cache.GetOrAddAsync("k", () => Task.FromResult(++calls));
        Assert.Equal(2, calls);
    }
}
