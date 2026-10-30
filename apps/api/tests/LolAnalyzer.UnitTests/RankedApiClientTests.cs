using System.Net;
using System.Text;
using LolAnalyzer.Application.Caching;
using LolAnalyzer.Application.Observability;
using LolAnalyzer.Application.Riot;
using LolAnalyzer.Infrastructure.Caching;
using LolAnalyzer.Infrastructure.Riot;
using Xunit;

namespace LolAnalyzer.UnitTests;

public sealed class RankedApiClientTests
{
    private const string Solo = """[{"queueType":"RANKED_SOLO_5x5","tier":"GOLD","rank":"II","leaguePoints":54}]""";
    private const string Both = """[{"queueType":"RANKED_FLEX_SR","tier":"SILVER","rank":"I","leaguePoints":12},{"queueType":"RANKED_SOLO_5x5","tier":"GOLD","rank":"II","leaguePoints":54}]""";

    [Fact]
    public async Task OnePlatformPuuidRequestReturnsBothQueuesWithAnObservationTime()
    {
        using var handler = new Handler((request, _) =>
        {
            Assert.Equal("euw1.api.riotgames.com", request.RequestUri!.Host);
            Assert.Equal("/lol/league/v4/entries/by-puuid/owner%2Fwith%20space", request.RequestUri.AbsolutePath);
            Assert.Equal("synthetic-test-key", request.Headers.GetValues("X-Riot-Token").Single());
            return Task.FromResult(Json(Both));
        });
        using var http = Http(handler);
        var clock = new Clock();
        var riot = Create(http, clock);
        var profile = await riot.ForPlatform("EUW1").GetRankedAsync("owner/with space", TestContext.Current.CancellationToken);
        Assert.Equal(["RANKED_SOLO_5x5", "RANKED_FLEX_SR"], profile.Entries.Select(entry => entry.QueueType));
        Assert.Equal(54, profile.Entries[0].LeaguePoints);
        Assert.Equal("II", profile.Entries[0].Division);
        Assert.Equal(clock.GetUtcNow(), profile.ObservedAt);
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task SuccessfulEmptyListIsCachedButErrorsAreNot()
    {
        using var handler = new Handler((_, _) => Task.FromResult(Json("[]")));
        using var http = Http(handler);
        var riot = Create(http);
        var first = await riot.GetRankedAsync("owner", TestContext.Current.CancellationToken);
        var second = await riot.GetRankedAsync("owner", TestContext.Current.CancellationToken);
        Assert.Empty(first.Entries);
        Assert.Same(first, second);
        Assert.Equal(1, handler.Calls);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task UpstreamFailureIsNotUnrankedAndIsNotCached(HttpStatusCode status)
    {
        using var handler = new Handler((_, _) => Task.FromResult(new HttpResponseMessage(status)));
        using var http = Http(handler);
        var riot = Create(http);
        for (var i = 0; i < 2; i++)
        {
            var error = await Assert.ThrowsAsync<RiotApiException>(() => riot.GetRankedAsync("owner", TestContext.Current.CancellationToken));
            Assert.Equal(status, error.StatusCode);
        }
        Assert.Equal(2, handler.Calls);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("[null]")]
    [InlineData("[{\"queueType\":\"RANKED_SOLO_5x5\",\"tier\":\"GOLD\",\"rank\":\"II\"}]")]
    [InlineData("[{\"queueType\":\"RANKED_SOLO_5x5\",\"tier\":\"UNKNOWN\",\"rank\":\"II\",\"leaguePoints\":54}]")]
    [InlineData("[{\"queueType\":\"RANKED_SOLO_5x5\",\"tier\":\"GOLD\",\"rank\":\"II\",\"leaguePoints\":54,\"puuid\":\"another-owner\"}]")]
    public async Task MalformedDataIsUnavailableRatherThanAnEmptyClassification(string payload)
    {
        using var handler = new Handler((_, _) => Task.FromResult(Json(payload)));
        using var http = Http(handler);
        var riot = Create(http);
        var error = await Assert.ThrowsAsync<RiotApiException>(() => riot.GetRankedAsync("owner", TestContext.Current.CancellationToken));
        Assert.Equal(HttpStatusCode.BadGateway, error.StatusCode);
    }

    [Fact]
    public async Task SignedLeaguePointsArePreservedAndDuplicateQueueIsRejected()
    {
        using var handler = new Handler((_, _) => Task.FromResult(Json(Solo.Replace("54", "-5", StringComparison.Ordinal))));
        using var http = Http(handler);
        Assert.Equal(-5, (await Create(http).GetRankedAsync("owner", TestContext.Current.CancellationToken)).Entries[0].LeaguePoints);
        using var duplicate = new Handler((_, _) => Task.FromResult(Json(Both.Replace("RANKED_FLEX_SR", "RANKED_SOLO_5x5", StringComparison.Ordinal))));
        using var duplicateHttp = Http(duplicate);
        await Assert.ThrowsAsync<RiotApiException>(() => Create(duplicateHttp).GetRankedAsync("owner", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CacheExpiresAndSeparatesPlatformAndPuuidWithoutPlainIdentityKeys()
    {
        using var handler = new Handler((_, _) => Task.FromResult(Json(Solo)));
        using var http = Http(handler);
        var clock = new Clock();
        var riot = Create(http, clock);
        await riot.GetRankedAsync("private-owner", TestContext.Current.CancellationToken);
        await riot.ForPlatform("LA1").GetRankedAsync("private-owner", TestContext.Current.CancellationToken);
        Assert.Equal(1, handler.Calls);
        await riot.ForPlatform("EUW1").GetRankedAsync("private-owner", TestContext.Current.CancellationToken);
        await riot.GetRankedAsync("another-owner", TestContext.Current.CancellationToken);
        Assert.Equal(3, handler.Calls);
        clock.Advance(TimeSpan.FromSeconds(301));
        await riot.GetRankedAsync("private-owner", TestContext.Current.CancellationToken);
        Assert.Equal(4, handler.Calls);
        Assert.DoesNotContain("private-owner", RiotCacheKeys.Ranked("private-owner"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task SingleFlightSharesWorkAndOneCancelledConsumerDoesNotCancelTheOther()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var handler = new Handler(async (_, token) =>
        {
            entered.TrySetResult();
            await release.Task.WaitAsync(token);
            return Json(Solo);
        });
        using var http = Http(handler);
        var riot = Create(http);
        using var cancellation = new CancellationTokenSource();
        var first = riot.GetRankedAsync("owner", cancellation.Token);
        await entered.Task.WaitAsync(TestContext.Current.CancellationToken);
        var second = riot.GetRankedAsync("owner", TestContext.Current.CancellationToken);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);
        release.TrySetResult();
        Assert.Single((await second).Entries);
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task AbandoningAllConsumersCancelsUpstreamAndAllowsRetry()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var count = 0;
        using var handler = new Handler(async (_, token) =>
        {
            if (Interlocked.Increment(ref count) > 1) return Json(Solo);
            entered.TrySetResult();
            try { await Task.Delay(Timeout.Infinite, token); }
            catch (OperationCanceledException) { cancelled.TrySetResult(); throw; }
            return Json(Solo);
        });
        using var http = Http(handler);
        var riot = Create(http);
        using var cancellation = new CancellationTokenSource();
        var request = riot.GetRankedAsync("owner", cancellation.Token);
        await entered.Task.WaitAsync(TestContext.Current.CancellationToken);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => request);
        await cancelled.Task.WaitAsync(TestContext.Current.CancellationToken);
        Assert.Single((await riot.GetRankedAsync("owner", TestContext.Current.CancellationToken)).Entries);
        Assert.Equal(2, handler.Calls);
    }

    [Fact]
    public async Task HttpTimeoutPropagatesWithoutReturningAnEmptySnapshot()
    {
        using var handler = new Handler(async (_, token) => { await Task.Delay(5000, token); return Json(Solo); });
        using var http = Http(handler);
        http.Timeout = TimeSpan.FromMilliseconds(50);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Create(http).GetRankedAsync("owner", TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("ph2")]
    [InlineData("th2")]
    [InlineData("me1")]
    [InlineData("invalid")]
    public async Task UnsupportedRankedPlatformsDoNotCallRiot(string platform)
    {
        using var handler = new Handler((_, _) => throw new InvalidOperationException("No HTTP expected"));
        using var http = Http(handler);
        var options = new RiotOptions { ApiKey = "synthetic-test-key", PlatformRegion = platform, MaxRetryAttempts = 0 };
        await Assert.ThrowsAsync<ArgumentException>(() => Create(http, options: options).GetRankedAsync("owner", TestContext.Current.CancellationToken));
        Assert.Equal(0, handler.Calls);
    }

    [Fact]
    public void SecondaryTimeoutAndCacheSettingsAreBounded()
    {
        new CacheOptions { RankedTtlSeconds = 300 }.Validate();
        Assert.Throws<InvalidOperationException>(() => new CacheOptions { RankedTtlSeconds = 0 }.Validate());
        Assert.Throws<InvalidOperationException>(() => new CacheOptions { RankedTtlSeconds = 901 }.Validate());
        Assert.Throws<InvalidOperationException>(() => new RiotOptions { RankedRequestTimeoutSeconds = 0 }.Validate());
        Assert.Throws<InvalidOperationException>(() => new RiotOptions { RankedRequestTimeoutSeconds = 6 }.Validate());
    }

    private static HttpClient Http(HttpMessageHandler handler) => new(handler) { MaxResponseContentBufferSize = 1024 * 1024 };
    private static HttpResponseMessage Json(string payload) => new(HttpStatusCode.OK) { Content = new StringContent(payload, Encoding.UTF8, "application/json") };
    private static RiotApiClient Create(HttpClient http, TimeProvider? clock = null, RiotOptions? options = null)
    {
        clock ??= TimeProvider.System;
        options ??= new RiotOptions { ApiKey = "synthetic-test-key", MaxRetryAttempts = 0 };
        var cache = new CacheOptions();
        return new RiotApiClient(http, options, new RiotRateLimiter(options, clock), clock, new OperationalMetrics(),
            new RiotResponseCache(new MemoryCacheService(clock, cache)), cache);
    }

    private sealed class Clock : TimeProvider
    {
        private DateTimeOffset now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => now;
        public void Advance(TimeSpan amount) => now += amount;
    }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        private int calls;
        public int Calls => calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Interlocked.Increment(ref calls);
            return send(request, token);
        }
    }
}
