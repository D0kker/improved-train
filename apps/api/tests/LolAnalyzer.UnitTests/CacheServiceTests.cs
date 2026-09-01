using LolAnalyzer.Application.Analysis;
using LolAnalyzer.Application.Caching;
using LolAnalyzer.Application.Observability;
using LolAnalyzer.Infrastructure.Caching;
using Xunit;

namespace LolAnalyzer.UnitTests;

public sealed class CacheServiceTests
{
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
