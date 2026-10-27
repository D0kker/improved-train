using System.Net;
using System.Net.Http.Headers;
using LolAnalyzer.Application.Caching;
using LolAnalyzer.Application.Observability;
using LolAnalyzer.Application.Riot;
using LolAnalyzer.Infrastructure.Caching;
using LolAnalyzer.Infrastructure.Riot;
using LolAnalyzer.Testing;
using Xunit;

namespace LolAnalyzer.UnitTests;

public sealed class RiotResourceProtectionTests
{
    [Fact]
    public async Task ProactiveWindowReopensAfterExpiry()
    {
        var clock = new AdjustableTimeProvider();
        using var limiter = new RiotRateLimiter(new RiotOptions { ShortWindowRequests = 1 }, clock);
        using (await limiter.AcquireAsync("americas", "match", TestContext.Current.CancellationToken)) { }
        clock.Advance(TimeSpan.FromSeconds(2));
        using var next = await limiter.AcquireAsync("americas", "match", TestContext.Current.CancellationToken);
        Assert.NotNull(next);
    }

    [Fact]
    public async Task ArbitrarilyLongCooldownRemainsCancellableWithoutTimerOverflow()
    {
        using var limiter = new RiotRateLimiter(new RiotOptions(), TimeProvider.System);
        limiter.RegisterRetryAfter(TimeSpan.MaxValue);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(30));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            using var lease = await limiter.AcquireAsync(cancellation.Token);
        });
    }

    [Fact]
    public async Task QuotasAreSharedAcrossMethodsButIsolatedByHost()
    {
        using var limiter = new RiotRateLimiter(new RiotOptions { ShortWindowRequests = 1 }, TimeProvider.System);
        using (await limiter.AcquireAsync("americas", "match", TestContext.Current.CancellationToken)) { }
        using (await limiter.AcquireAsync("europe", "match", TestContext.Current.CancellationToken)) { }
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(30));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            using var lease = await limiter.AcquireAsync("americas", "account", cancellation.Token);
        });
    }

    [Fact]
    public async Task MethodHeaderExhaustionDoesNotBlockUnrelatedMethod()
    {
        using var limiter = new RiotRateLimiter(new RiotOptions(), TimeProvider.System);
        limiter.ObserveLimits("americas", "match", null, null, "1:1", "1:1");
        using (await limiter.AcquireAsync("americas", "account", TestContext.Current.CancellationToken)) { }
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(30));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            using var lease = await limiter.AcquireAsync("americas", "match", cancellation.Token);
        });
    }

    [Fact]
    public async Task AppHeadersAreLearnedAcrossPlatformClientClones()
    {
        var calls = 0;
        using var handler = new AsyncHandler((_, _) =>
        {
            Interlocked.Increment(ref calls);
            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("[]") };
            response.Headers.Add("X-App-Rate-Limit", "1:1");
            response.Headers.Add("X-App-Rate-Limit-Count", "1:1");
            return Task.FromResult(response);
        });
        using var http = new HttpClient(handler);
        using var metrics = new OperationalMetrics();
        using var limiter = new RiotRateLimiter(new RiotOptions(), TimeProvider.System);
        var client = Client(http, limiter, metrics);
        await client.GetMatchIdsAsync("first", 0, 5, TestContext.Current.CancellationToken);
        using var cancellation = new CancellationTokenSource();
        var pending = client.ForPlatform("LA1").GetMatchIdsAsync("second", 0, 5, cancellation.Token);
        Assert.Equal(1, calls);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task ServiceUnavailableIsNotRetried()
    {
        var calls = 0;
        using var handler = new AsyncHandler((_, _) =>
        {
            Interlocked.Increment(ref calls);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        });
        using var http = new HttpClient(handler);
        using var metrics = new OperationalMetrics();
        using var limiter = new RiotRateLimiter(new RiotOptions(), TimeProvider.System);
        var exception = await Assert.ThrowsAsync<RiotApiException>(() => Client(http, limiter, metrics).GetMatchIdsAsync("first", 0, 5, TestContext.Current.CancellationToken));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, exception.StatusCode);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task LongRetryAfterIsNeverTruncatedAndBlocksOtherCalls()
    {
        var calls = 0;
        using var handler = new AsyncHandler((_, _) =>
        {
            Interlocked.Increment(ref calls);
            var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(2));
            return Task.FromResult(response);
        });
        using var http = new HttpClient(handler);
        using var metrics = new OperationalMetrics();
        var options = new RiotOptions { ApiKey = "fixture", MaxRetryDelaySeconds = 1 };
        using var limiter = new RiotRateLimiter(options, TimeProvider.System);
        var client = Client(http, limiter, metrics, options);
        await Assert.ThrowsAsync<RiotApiException>(() => client.GetMatchIdsAsync("first", 0, 5, TestContext.Current.CancellationToken));
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(30));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.GetMatchIdsAsync("second", 0, 5, cancellation.Token));
        Assert.Equal(1, calls);
        Assert.Equal(1, metrics.Snapshot().RiotRateLimits);
    }

    [Fact]
    public async Task SlowTransportReceivesCancellationAndDoesNotRetry()
    {
        var calls = 0;
        var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var handler = new AsyncHandler(async (_, token) =>
        {
            Interlocked.Increment(ref calls);
            try { await Task.Delay(Timeout.InfiniteTimeSpan, token); }
            finally { cancelled.TrySetResult(); }
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        using var http = new HttpClient(handler) { Timeout = TimeSpan.FromMilliseconds(30) };
        using var metrics = new OperationalMetrics();
        using var limiter = new RiotRateLimiter(new RiotOptions(), TimeProvider.System);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Client(http, limiter, metrics).GetMatchIdsAsync("first", 0, 5, TestContext.Current.CancellationToken));
        await cancelled.Task.WaitAsync(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task MalformedQuotaHeadersAreIgnoredWithoutUnboundedParsing()
    {
        using var limiter = new RiotRateLimiter(new RiotOptions(), TimeProvider.System);
        limiter.ObserveLimits("americas", "match", new string('x', 1000), "bad", "1:1,2:1,garbage,0:0", "1:1,2:1");
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(30));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            using var lease = await limiter.AcquireAsync("americas", "match", cancellation.Token);
        });
    }

    private static RiotApiClient Client(HttpClient http, RiotRateLimiter limiter, OperationalMetrics metrics, RiotOptions? options = null)
    {
        var cache = new CacheOptions { Enabled = false };
        return new RiotApiClient(http, options ?? new RiotOptions { ApiKey = "fixture" }, limiter, TimeProvider.System,
            metrics, new RiotResponseCache(new MemoryCacheService(TimeProvider.System, cache)), cache);
    }

    private sealed class AsyncHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => send(request, cancellationToken);
    }
}
