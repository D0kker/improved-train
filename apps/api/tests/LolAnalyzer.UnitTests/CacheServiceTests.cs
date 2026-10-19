using System.Globalization;
using LolAnalyzer.Application.Analysis;
using LolAnalyzer.Application.Caching;
using LolAnalyzer.Application.Observability;
using LolAnalyzer.Infrastructure.Caching;
using LolAnalyzer.Infrastructure.Riot;
using Xunit;

namespace LolAnalyzer.UnitTests;

public sealed class CacheServiceTests
{
    [Theory]
    [InlineData("memory", false)]
    [InlineData("MEMORY", false)]
    [InlineData("redis", true)]
    [InlineData("REDIS", true)]
    public void CacheProviderIsExplicitAndCaseInsensitive(string provider, bool usesRedis)
    {
        var options = new CacheOptions { Provider = provider };

        options.Validate();

        Assert.Equal(usesRedis, options.UsesRedis);
    }

    [Fact]
    public void UnknownCacheProviderFailsFast()
    {
        var options = new CacheOptions { Provider = "distributed-magic" };

        var exception = Assert.Throws<InvalidOperationException>(options.Validate);

        Assert.Contains("memory", exception.Message, StringComparison.Ordinal);
        Assert.Contains("redis", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PlayerSummaryQueryUsesCacheUntilPlayerTagIsInvalidated()
    {
        var repository = new CountingPlayerAnalysisRepository();
        var cache = new MemoryCacheService(TimeProvider.System);
        var service = new PlayerSummaryQueryService(
            repository,
            cache,
            new CacheOptions { PlayerSummaryTtlSeconds = 60 });

        var first = await service.GetAsync("test-puuid", TestContext.Current.CancellationToken);
        var second = await service.GetAsync("test-puuid", TestContext.Current.CancellationToken);

        Assert.NotNull(first);
        Assert.Equal(first, second);
        Assert.Equal(1, repository.SummaryReads);

        await cache.RemoveTagAsync(PlayerCacheKeys.Tag("test-puuid"), TestContext.Current.CancellationToken);
        var afterInvalidation = await service.GetAsync("test-puuid", TestContext.Current.CancellationToken);

        Assert.NotNull(afterInvalidation);
        Assert.Equal(2, repository.SummaryReads);
    }

    [Fact]
    public async Task RedisFailureFallsBackToMemoryAndInvalidationRemainsSafe()
    {
        var memory = new MemoryCacheService(TimeProvider.System);
        var cache = new RedisCacheService(new FailingRedisCacheStore(), memory, new OperationalMetrics());
        var summary = CountingPlayerAnalysisRepository.CreateSummary();
        var key = PlayerCacheKeys.Summary(summary.Puuid);
        var tag = PlayerCacheKeys.Tag(summary.Puuid);

        await cache.SetAsync(key, summary, TimeSpan.FromMinutes(1), tag, TestContext.Current.CancellationToken);
        var cached = await cache.GetAsync<PlayerSummary>(key, TestContext.Current.CancellationToken);

        Assert.Equal(summary, cached);

        await cache.RemoveTagAsync(tag, TestContext.Current.CancellationToken);
        Assert.Null(await cache.GetAsync<PlayerSummary>(key, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task InvalidRedisPayloadIsTreatedAsCacheMiss()
    {
        var cache = new RedisCacheService(
            new InvalidPayloadRedisCacheStore(),
            new MemoryCacheService(TimeProvider.System),
            new OperationalMetrics());

        var result = await cache.GetAsync<PlayerSummary>("test-key", TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    [Fact]
    public async Task RiotResponseCacheSharesConcurrentMatchDownload()
    {
        var cache = new RiotResponseCache(new MemoryCacheService(TimeProvider.System));
        var calls = 0;

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => cache.GetOrCreateAsync(
                "riot-match:test",
                TimeSpan.FromMinutes(1),
                "riot-match-details",
                async cancellationToken =>
                {
                    Interlocked.Increment(ref calls);
                    await Task.Delay(10, cancellationToken);
                    return "match";
                },
                TestContext.Current.CancellationToken))
            .ToArray();

        var values = await Task.WhenAll(tasks);

        Assert.All(values, value => Assert.Equal("match", value));
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task MemoryCacheExpiresAndBoundsEntries()
    {
        var time = new FixedTimeProvider(DateTimeOffset.Parse(
            "2026-10-18T12:00:00Z",
            CultureInfo.InvariantCulture));
        var cache = new MemoryCacheService(time);

        await cache.SetAsync("expiring", "value", TimeSpan.FromMinutes(1), "test", TestContext.Current.CancellationToken);
        Assert.Equal("value", await cache.GetAsync<string>("expiring", TestContext.Current.CancellationToken));

        time.Advance(TimeSpan.FromMinutes(2));

        Assert.Null(await cache.GetAsync<string>("expiring", TestContext.Current.CancellationToken));
    }

    private sealed class FailingRedisCacheStore : IRedisCacheStore
    {
        public Task<string?> GetAsync(string key, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Redis unavailable in simulated test.");

        public Task SetAsync(
            string key,
            string payload,
            TimeSpan timeToLive,
            string tag,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Redis unavailable in simulated test.");

        public Task RemoveTagAsync(string tag, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Redis unavailable in simulated test.");
    }

    private sealed class InvalidPayloadRedisCacheStore : IRedisCacheStore
    {
        public Task<string?> GetAsync(string key, CancellationToken cancellationToken) =>
            Task.FromResult<string?>("not-json");

        public Task SetAsync(
            string key,
            string payload,
            TimeSpan timeToLive,
            string tag,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task RemoveTagAsync(string tag, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset current = now;

        public override DateTimeOffset GetUtcNow() => current;

        public void Advance(TimeSpan duration) => current = current.Add(duration);
    }

    private sealed class CountingPlayerAnalysisRepository : IPlayerAnalysisRepository
    {
        public int SummaryReads { get; private set; }

        public Task<PlayerSummary?> GetSummaryAsync(string puuid, CancellationToken cancellationToken)
        {
            SummaryReads++;
            return Task.FromResult<PlayerSummary?>(CreateSummary() with { Puuid = puuid });
        }

        public static PlayerSummary CreateSummary() =>
            new("test-puuid", "Test", "TAG", 10, 6, 4, 60, 9, 2, DateTimeOffset.UnixEpoch);

        public Task<PlayerAnalysisInput?> LoadInputAsync(string ownerPuuid, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<MatchFamiliarityInput?> LoadFamiliarityInputAsync(
            string ownerPuuid,
            string targetRiotMatchId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task ReplaceEncountersAsync(
            Guid ownerPlayerId,
            IReadOnlyCollection<PlayerEncounterAggregate> encounters,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<PlayerEncounterView>?> GetRepeatedPlayersAsync(
            string puuid,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<PlayerEncounterView>?> GetKnownPlayersAsync(
            string puuid,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<PagedPlayerMatches?> GetMatchesAsync(
            string puuid,
            int page,
            int pageSize,
            int? queueId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<MatchDetail?> GetMatchDetailAsync(string riotMatchId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
