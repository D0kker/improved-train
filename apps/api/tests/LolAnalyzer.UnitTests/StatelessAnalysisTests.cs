using System.Text.Json;
using LolAnalyzer.Application.Caching;
using LolAnalyzer.Application.Riot;
using LolAnalyzer.Application.Stateless;
using LolAnalyzer.Infrastructure.Caching;
using LolAnalyzer.Infrastructure.Riot;
using LolAnalyzer.Testing;
using Xunit;

namespace LolAnalyzer.UnitTests;

public sealed class StatelessAnalysisTests
{
    [Fact]
    public void EvidenceOmitsMissingIdentityAndDoesNotInventTeams()
    {
        var match = StatelessFixture.Match("LA1_TARGET");
        var broken = match with { Participants = match.Participants.Select((p, i) => i == 1 ? p with { Puuid = "" } : i == 2 ? p with { TeamId = 0 } : p).ToArray() };
        var evidence = RequestAnalysisCalculator.Evidence(broken, "owner", "LA1");
        Assert.Equal("partial", evidence.Status);
        Assert.Equal(9, evidence.Participants.Count);
        Assert.Null(Assert.Single(evidence.Participants, p => p.Puuid == "p2").TeamId);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(29, 29)]
    [InlineData(-1, null)]
    public async Task HistoricalIconAndEvidenceReuseExistingDownloads(int? input, int? expected)
    {
        var fixture = new StatelessFixture { ProfileIconId = input };
        var result = await StatelessFixture.Service(fixture).SearchAsync("Fixture", "TEST", "LA1", TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal(expected, result.Player.ProfileIconId);
        Assert.Equal(5, fixture.Details.Count);
        Assert.All(result.Matches.Items, row => Assert.Equal(10, row.Evidence!.Participants.Count));
    }

    [Theory]
    [InlineData(5, 5)]
    [InlineData(1, 2)]
    [InlineData(0, 1)]
    public async Task SearchReturnsAtMostFiveMatchesWithT03Statistics(int available, int expected)
    {
        var fixture = new StatelessFixture { HistoryLength = available };
        var result = await StatelessFixture.Service(fixture).SearchAsync("Fixture User", "TEST", "LA1", TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal(expected, result.Matches.Items.Count);
        Assert.Equal(expected, result.Summary.MatchesAnalyzed);
        Assert.Equal(expected * 9, result.Summary.OtherPlayerParticipations);
        Assert.Equal(9, result.Summary.UniquePlayersEncountered);
        Assert.Equal(expected >= 2 ? 9 : 0, result.Summary.RepeatedPlayers);
        Assert.Equal(expected, fixture.Details.Count);
        Assert.DoesNotContain(fixture.Lists, list => list.EndTime is not null);
    }

    [Fact]
    public async Task DirectAnalysisDoesNotNeedSearchAndRequestsFivePlusThreePerOtherParticipant()
    {
        var fixture = new StatelessFixture();
        var result = await StatelessFixture.Service(fixture).AnalyzeAsync("LA1_TARGET", "owner", "LA1", TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal(0, fixture.AccountCalls);
        Assert.Equal(10, fixture.Lists.Count);
        Assert.Equal(6, Assert.Single(fixture.Lists, list => list.Puuid == "owner").Count);
        Assert.All(fixture.Lists.Where(list => list.Puuid != "owner"), list => Assert.Equal(4, list.Count));
        Assert.All(fixture.Lists, list => Assert.Equal(StatelessFixture.TargetTime.ToUnixTimeSeconds(), list.EndTime));
        Assert.Equal(5, Assert.Single(result.Coverage.Players, player => player.Puuid == "owner").EligibleMatches);
        Assert.All(result.Coverage.Players.Where(player => player.Puuid != "owner"), player => Assert.Equal(3, player.EligibleMatches));
        Assert.Equal("complete", result.Coverage.Status);
        Assert.Equal(5, result.Coverage.UniqueMatchIds);
        Assert.Equal(6, fixture.Details.Count);
        Assert.All(fixture.Details.Values, count => Assert.Equal(1, count));
        Assert.Equal(5, result.Summary.MatchesAnalyzed);
        Assert.Equal(3, result.Summary.Wins);
        Assert.Equal(2, result.Summary.Losses);
        Assert.Equal(60, result.Summary.WinRate);
        Assert.Equal(45, result.Summary.OtherPlayerParticipations);
        Assert.Equal(9, result.Match.Familiarity!.KnownPlayers);
        Assert.Equal(9, result.Match.Familiarity.EvaluablePlayers);
        Assert.Equal(100, result.Match.Familiarity.FamiliarityPercentage);
        Assert.Equal(45, result.Relationships.Count);
        Assert.All(result.Relationships, pair => Assert.Equal(5, pair.MatchesTogether));
        Assert.All(result.Match.Familiarity.KnownPlayerDetails, known => Assert.Equal(5, known.PlayedBefore));
        Assert.Equal(2, result.Match.PremadeGroups.Count);
    }

    [Fact]
    public async Task DistinctHistoriesProduceExactlyThirtyTwoUniqueAdditionalMatches()
    {
        var fixture = new StatelessFixture { SeparateHistories = true };
        var result = await StatelessFixture.Service(fixture).AnalyzeAsync("LA1_TARGET", "owner", "LA1", TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal(32, result.Coverage.UniqueMatchIds);
        Assert.Equal(32, result.Coverage.MatchesRecovered);
        Assert.Equal(33, fixture.Details.Count);
        Assert.Equal(0, result.Match.Familiarity!.KnownPlayers);
        Assert.Empty(result.Relationships);
        Assert.Equal("complete", result.Coverage.Status);
    }

    [Fact]
    public async Task FutureMatchesAreDiscardedEvenIfUpstreamIgnoresEndTime()
    {
        var fixture = new StatelessFixture { InjectFuture = true };
        var result = await StatelessFixture.Service(fixture).AnalyzeAsync("LA1_TARGET", "owner", "LA1", TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal("partial", result.Coverage.Status);
        Assert.All(result.Relationships, pair => Assert.True(pair.LastSeenAt < StatelessFixture.TargetTime));
        Assert.All(result.Match.Familiarity!.KnownPlayerDetails, player => Assert.True(player.LastEncounterAt < StatelessFixture.TargetTime));
    }

    [Fact]
    public async Task ConsecutiveAnalysesAreIsolatedAndDeterministic()
    {
        var service = StatelessFixture.Service(new StatelessFixture());
        var first = await service.AnalyzeAsync("LA1_TARGET", "owner", "LA1", TestContext.Current.CancellationToken);
        var second = await service.AnalyzeAsync("LA1_ALT", "alt-owner", "LA1", TestContext.Current.CancellationToken);
        var repeated = await service.AnalyzeAsync("LA1_TARGET", "owner", "LA1", TestContext.Current.CancellationToken);
        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(JsonSerializer.Serialize(first), JsonSerializer.Serialize(repeated));
        Assert.All(second.Encounters, player => Assert.StartsWith("alt-", player.OtherPlayerPuuid));
        Assert.All(first.Encounters, player => Assert.DoesNotContain("alt-", player.OtherPlayerPuuid, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("p1", null)]
    [InlineData(null, "LA1_H2")]
    public async Task FailuresPreserveOtherEvidenceAndMarkPartialCoverage(string? failingPlayer, string? failingMatch)
    {
        var fixture = new StatelessFixture { FailingPuuid = failingPlayer, FailingMatch = failingMatch };
        var result = await StatelessFixture.Service(fixture).AnalyzeAsync("LA1_TARGET", "owner", "LA1", TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal("partial", result.Coverage.Status);
        Assert.NotEmpty(result.Coverage.Failures);
        Assert.True(result.Coverage.MatchesRecovered > 0);
    }

    [Fact]
    public async Task RateLimitsCannotProduceDefinitiveZeroKnownPlayers()
    {
        var result = await StatelessFixture.Service(new StatelessFixture { RateLimited = true })
            .AnalyzeAsync("LA1_TARGET", "owner", "LA1", TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal("error", result.Coverage.Status);
        Assert.All(result.Coverage.Failures, failure => Assert.Equal("riot_rate_limited", failure.Code));
    }

    [Fact]
    public async Task RequestCancellationStopsRiotWork()
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(20));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => StatelessFixture.Service(new StatelessFixture { DelayMilliseconds = 200 })
            .AnalyzeAsync("LA1_TARGET", "owner", "LA1", cancellation.Token));
    }

    [Fact]
    public async Task EmptyExpiredAndDisabledCachesNeverChangeResults()
    {
        var time = new AdjustableTimeProvider();
        var fixture = new StatelessFixture();
        var cache = new MemoryCacheService(time);
        var service = StatelessFixture.Service(new CachedFixtureClient(fixture, new RiotResponseCache(cache)));
        var first = await service.AnalyzeAsync("LA1_TARGET", "owner", "LA1", TestContext.Current.CancellationToken);
        var calls = fixture.Details.Values.Sum();
        var warm = await service.AnalyzeAsync("LA1_TARGET", "owner", "LA1", TestContext.Current.CancellationToken);
        Assert.Equal(calls, fixture.Details.Values.Sum());
        time.Advance(TimeSpan.FromMinutes(61));
        var expired = await service.AnalyzeAsync("LA1_TARGET", "owner", "LA1", TestContext.Current.CancellationToken);
        Assert.Equal(calls * 2, fixture.Details.Values.Sum());
        var disabled = StatelessFixture.Service(new CachedFixtureClient(fixture,
            new RiotResponseCache(new MemoryCacheService(time, new CacheOptions { Enabled = false }))));
        var uncached = await disabled.AnalyzeAsync("LA1_TARGET", "owner", "LA1", TestContext.Current.CancellationToken);
        Assert.Equal(JsonSerializer.Serialize(first), JsonSerializer.Serialize(warm));
        Assert.Equal(JsonSerializer.Serialize(first), JsonSerializer.Serialize(expired));
        Assert.Equal(JsonSerializer.Serialize(first), JsonSerializer.Serialize(uncached));
    }

    [Fact]
    public async Task SimultaneousAnalysesShareExternalDownloadsAndOneCallerCanCancel()
    {
        var fixture = new StatelessFixture { DelayMilliseconds = 10 };
        var service = StatelessFixture.Service(new CachedFixtureClient(fixture, new RiotResponseCache(new MemoryCacheService(TimeProvider.System))));
        var results = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => service.AnalyzeAsync("LA1_TARGET", "owner", "LA1", TestContext.Current.CancellationToken)));
        Assert.All(results, result => Assert.NotNull(result));
        Assert.All(fixture.Details.Values, calls => Assert.Equal(1, calls));
        Assert.Equal(10, fixture.Lists.Count);
        var cache = new RiotResponseCache(new MemoryCacheService(TimeProvider.System));
        using var cancellation = new CancellationTokenSource();
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<string?> Fetch(CancellationToken token) { ready.TrySetResult(); await release.Task.WaitAsync(token); return "value"; }
        var cancelled = cache.GetOrCreateAsync("shared", TimeSpan.FromMinutes(1), "t", Fetch, cancellation.Token);
        await ready.Task.WaitAsync(TestContext.Current.CancellationToken);
        var surviving = cache.GetOrCreateAsync("shared", TimeSpan.FromMinutes(1), "t", Fetch, TestContext.Current.CancellationToken);
        cancellation.Cancel();
        release.TrySetResult();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelled);
        Assert.Equal("value", await surviving);
    }

    [Fact]
    public async Task SoleCallerCancellationReleasesSingleFlightAndAllowsRetry()
    {
        var cache = new RiotResponseCache(new MemoryCacheService(TimeProvider.System));
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cancellation = new CancellationTokenSource();
        async Task<string?> Fetch(CancellationToken token)
        {
            ready.TrySetResult();
            try { await Task.Delay(Timeout.InfiniteTimeSpan, token); }
            finally { stopped.TrySetResult(); }
            return "unused";
        }
        var request = cache.GetOrCreateAsync("cancelled", TimeSpan.FromMinutes(1), "t", Fetch, cancellation.Token);
        await ready.Task.WaitAsync(TestContext.Current.CancellationToken);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => request);
        await stopped.Task.WaitAsync(TestContext.Current.CancellationToken);
        Assert.Equal("retry", await cache.GetOrCreateAsync("cancelled", TimeSpan.FromMinutes(1), "t",
            _ => Task.FromResult<string?>("retry"), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CacheFailureDoesNotPreventAnIndependentAnalysis()
    {
        var result = await StatelessFixture.Service(new CachedFixtureClient(new StatelessFixture(), new RiotResponseCache(new BrokenCache())))
            .AnalyzeAsync("LA1_TARGET", "owner", "LA1", TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal("complete", result.Coverage.Status);
        Assert.Equal(9, result.Match.Familiarity!.KnownPlayers);
    }

    [Fact]
    public async Task CacheEnforcesEntryAndByteLimitsUnderConcurrentWrites()
    {
        var settings = new CacheOptions { MaxMemoryEntries = 128, MaxMemoryBytes = 1024 * 1024 };
        var cache = new MemoryCacheService(TimeProvider.System, settings);
        var token = TestContext.Current.CancellationToken;
        await Task.WhenAll(Enumerable.Range(0, 300).Select(index => Task.Run(() => cache.SetAsync(index.ToString(System.Globalization.CultureInfo.InvariantCulture),
            new string('x', 16_384), TimeSpan.FromMinutes(1), "fixed-tag", token), token)));
        Assert.InRange(cache.Count, 0, settings.MaxMemoryEntries);
        Assert.InRange(cache.EstimatedBytes, 0, settings.MaxMemoryBytes);
    }

    private sealed class BrokenCache : ICacheService
    {
        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken) where T : class => throw new InvalidOperationException("Simulated cache failure");
        public Task SetAsync<T>(string key, T value, TimeSpan timeToLive, string tag, CancellationToken cancellationToken) where T : class => throw new InvalidOperationException("Simulated cache failure");
        public Task RemoveTagAsync(string tag, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
