using System.Runtime.CompilerServices;
using LolAnalyzer.Application.Caching;
using LolAnalyzer.Application.Stateless;
using LolAnalyzer.Infrastructure.Caching;
using LolAnalyzer.Infrastructure.Riot;
using LolAnalyzer.Testing;
using Xunit;

namespace LolAnalyzer.UnitTests;

public sealed class ResourceMemoryTests
{
    [Fact]
    public async Task CompletedAnalysisIsCollectibleWhileCacheAndServiceRemainAlive()
    {
        var cache = new MemoryCacheService(TimeProvider.System);
        var responses = new RiotResponseCache(cache);
        var service = StatelessFixture.Service(new CachedFixtureClient(new StatelessFixture(), responses));
        var reference = await CompleteAsync(service, TestContext.Current.CancellationToken);
        for (var attempt = 0; attempt < 3 && reference.IsAlive; attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            await Task.Yield();
        }
        Assert.False(reference.IsAlive);
        Assert.True(cache.Count > 0); // Only optional Riot DTOs survive, not functional analysis results.
        Assert.Equal(0, responses.InFlightCount);
        GC.KeepAlive(service);
        GC.KeepAlive(responses);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<WeakReference> CompleteAsync(OnDemandAnalysisService service, CancellationToken token)
    {
        var result = await service.AnalyzeAsync("LA1_TARGET", "owner", "LA1", token);
        return new WeakReference(result);
    }

    [Fact]
    public async Task CacheEvictionDoesNotBreakPendingSingleFlightAndExpiryReclaimsEntries()
    {
        var clock = new AdjustableTimeProvider();
        var cache = new MemoryCacheService(clock, new CacheOptions { MaxMemoryEntries = 128, MaxMemoryBytes = 1024 * 1024 });
        var flights = new RiotResponseCache(cache);
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        async Task<string?> Fetch(CancellationToken token)
        {
            Interlocked.Increment(ref calls);
            ready.SetResult();
            await release.Task.WaitAsync(token);
            return "shared-result";
        }
        var token = TestContext.Current.CancellationToken;
        var first = flights.GetOrCreateAsync("pending", TimeSpan.FromMinutes(1), "t", Fetch, token);
        await ready.Task.WaitAsync(token);
        for (var index = 0; index < 256; index++)
            await cache.SetAsync(index.ToString(System.Globalization.CultureInfo.InvariantCulture), new string('x', 1024), TimeSpan.FromMinutes(1), "t", token);
        var second = flights.GetOrCreateAsync("pending", TimeSpan.FromMinutes(1), "t", Fetch, token);
        release.SetResult();
        Assert.All(await Task.WhenAll(first, second), value => Assert.Equal("shared-result", value));
        Assert.Equal(1, calls);
        Assert.Equal(0, flights.InFlightCount);
        Assert.InRange(cache.Count, 1, 128);
        Assert.InRange(cache.EstimatedBytes, 1, 1024 * 1024);
        clock.Advance(TimeSpan.FromMinutes(2));
        await cache.SetAsync("fresh", "value", TimeSpan.FromMinutes(1), "t", token);
        Assert.Equal(1, cache.Count);
    }

    [Fact]
    public async Task DelayedStaleCacheMissCannotStartASecondDownloadAfterCompletedFlight()
    {
        var cache = new DelayedMissCache();
        var flights = new RiotResponseCache(cache);
        var releaseFactory = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var token = TestContext.Current.CancellationToken;
        var calls = 0;
        async Task<string?> Fetch(CancellationToken ct)
        {
            Interlocked.Increment(ref calls);
            await releaseFactory.Task.WaitAsync(ct);
            return "value";
        }
        var first = flights.GetOrCreateAsync("id", TimeSpan.FromMinutes(1), "t", Fetch, token);
        var second = flights.GetOrCreateAsync("id", TimeSpan.FromMinutes(1), "t", Fetch, token);
        await cache.StaleReadStarted.Task.WaitAsync(token);
        releaseFactory.SetResult();
        Assert.Equal("value", await first);
        cache.ReleaseStaleRead.SetResult();
        Assert.Equal("value", await second);
        Assert.Equal(1, calls);
    }

    private sealed class DelayedMissCache : ICacheService
    {
        private int reads;
        private object? stored;
        public TaskCompletionSource StaleReadStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ReleaseStaleRead { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken) where T : class
        {
            if (Interlocked.Increment(ref reads) == 3)
            {
                StaleReadStarted.SetResult();
                await ReleaseStaleRead.Task.WaitAsync(cancellationToken);
                return null;
            }
            return stored as T;
        }
        public Task SetAsync<T>(string key, T value, TimeSpan timeToLive, string tag, CancellationToken cancellationToken) where T : class
        { stored = value; return Task.CompletedTask; }
        public Task RemoveTagAsync(string tag, CancellationToken cancellationToken) { stored = null; return Task.CompletedTask; }
    }
}
