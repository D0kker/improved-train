using System.Net;
using System.Text;
using LolAnalyzer.Application.Observability;
using LolAnalyzer.Infrastructure.Riot;
using Xunit;

namespace LolAnalyzer.UnitTests;

public sealed class RiotApiClientTests
{
    [Fact]
    public async Task GetAccountByRiotIdAsyncUsesRegionalEndpointEncodedPathAndTokenHeader()
    {
        using var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"puuid":"puuid-123","gameName":"Ana Uno","tagLine":"LAN"}""",
                Encoding.UTF8,
                "application/json"),
        });
        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://americas.api.riotgames.com/"),
        };
        var riotClient = CreateRiotClient(client, new RiotOptions
        {
            ApiKey = "unit-test-key",
            RegionalRouting = "americas",
        });

        var account = await riotClient.GetAccountByRiotIdAsync("Ana Uno", "LAN", TestContext.Current.CancellationToken);

        Assert.NotNull(account);
        Assert.Equal("puuid-123", account.Puuid);
        Assert.Equal("Ana Uno", account.GameName);
        Assert.Equal("/riot/account/v1/accounts/by-riot-id/Ana%20Uno/LAN", handler.Request!.RequestUri!.AbsolutePath);
        Assert.Equal("unit-test-key", handler.Request.Headers.GetValues("X-Riot-Token").Single());
    }

    [Fact]
    public async Task GetAccountByRiotIdAsyncReturnsNullForNotFound()
    {
        using var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://americas.api.riotgames.com/"),
        };
        var riotClient = CreateRiotClient(client, new RiotOptions { ApiKey = "unit-test-key" });

        var account = await riotClient.GetAccountByRiotIdAsync("Unknown", "LAN", TestContext.Current.CancellationToken);

        Assert.Null(account);
    }

    [Fact]
    public async Task GetMatchIdsAsyncUsesEncodedPuuidAndBoundedPagingQuery()
    {
        using var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[\"TEST_1\",\"TEST_2\"]", Encoding.UTF8, "application/json"),
        });
        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://americas.api.riotgames.com/"),
        };
        var riotClient = CreateRiotClient(client, new RiotOptions { ApiKey = "unit-test-key" });

        var matchIds = await riotClient.GetMatchIdsAsync("puuid/with space", 2, 2, TestContext.Current.CancellationToken);

        Assert.Equal(["TEST_1", "TEST_2"], matchIds);
        Assert.Equal(
            "/lol/match/v5/matches/by-puuid/puuid%2Fwith%20space/ids?start=2&count=2",
            handler.Request!.RequestUri!.PathAndQuery);
    }

    [Fact]
    public async Task GetMatchAsyncPreservesRawJsonAndNormalizesACompletedMatch()
    {
        const string responseBody = """
            {"metadata":{"matchId":"TEST_1"},"info":{"queueId":420,"gameCreation":1700000000000,"gameStartTimestamp":1700000010000,"gameEndTimestamp":1700001210000,"gameDuration":1200,"gameVersion":"test","participants":[{"puuid":"test-puuid","riotIdGameName":"Test","riotIdTagline":"TAG","teamId":100,"participantId":1,"championId":1,"championName":"Annie","teamPosition":"MIDDLE","individualPosition":"MIDDLE","kills":1,"deaths":2,"assists":3,"win":true,"goldEarned":1000,"totalDamageDealtToChampions":2000,"visionScore":10,"totalMinionsKilled":50,"neutralMinionsKilled":5}]}}
            """;
        using var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseBody, Encoding.UTF8, "application/json"),
        });
        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://americas.api.riotgames.com/"),
        };
        var riotClient = CreateRiotClient(client, new RiotOptions { ApiKey = "unit-test-key" });

        var match = await riotClient.GetMatchAsync("TEST_1", TestContext.Current.CancellationToken);

        Assert.NotNull(match);
        Assert.Equal("TEST_1", match.RiotMatchId);
        Assert.Equal(responseBody, match.RawJson);
        Assert.Equal(55, Assert.Single(match.Participants).Cs);
    }

    [Fact]
    public async Task RateLimitResponseRetriesOnceAfterRetryAfterThenSucceeds()
    {
        using var handler = new StubHttpMessageHandler((_, callCount) =>
        {
            if (callCount == 1)
            {
                var limited = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
                limited.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.Zero);
                return limited;
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"puuid":"puuid-123","gameName":"Ana","tagLine":"LAN"}""",
                    Encoding.UTF8,
                    "application/json"),
            };
        });
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://americas.api.riotgames.com/") };
        var options = new RiotOptions
        {
            ApiKey = "unit-test-key",
            BaseRetryDelayMilliseconds = 1,
            MaxRetryAttempts = 2,
        };
        var riotClient = CreateRiotClient(client, options);

        var account = await riotClient.GetAccountByRiotIdAsync("Ana", "LAN", TestContext.Current.CancellationToken);

        Assert.NotNull(account);
        Assert.Equal(2, handler.CallCount);
    }

    [Fact]
    public async Task RateLimitResponseDoesNotRetryBeyondConfiguredBound()
    {
        using var handler = new StubHttpMessageHandler((_, _) =>
        {
            var limited = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            limited.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(2));
            return limited;
        });
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://americas.api.riotgames.com/") };
        var options = new RiotOptions
        {
            ApiKey = "unit-test-key",
            MaxRetryAttempts = 2,
            MaxRetryDelaySeconds = 1,
        };
        var riotClient = CreateRiotClient(client, options);

        var exception = await Assert.ThrowsAsync<RiotApiException>(() =>
            riotClient.GetAccountByRiotIdAsync("Ana", "LAN", TestContext.Current.CancellationToken));

        Assert.Equal(HttpStatusCode.TooManyRequests, exception.StatusCode);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task RateLimiterBoundsConcurrencyAndReleasesWaitingRequest()
    {
        using var limiter = new RiotRateLimiter(
            new RiotOptions { RequestConcurrency = 1 },
            TimeProvider.System);
        using var firstLease = await limiter.AcquireAsync(TestContext.Current.CancellationToken);

        var secondLeaseTask = limiter.AcquireAsync(TestContext.Current.CancellationToken).AsTask();
        Assert.False(secondLeaseTask.IsCompleted);

        firstLease.Dispose();
        using var secondLease = await secondLeaseTask;
        Assert.True(secondLeaseTask.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task RateLimiterCooldownWaitIsCooperativelyCancellable()
    {
        using var limiter = new RiotRateLimiter(
            new RiotOptions { RequestConcurrency = 1 },
            TimeProvider.System);
        limiter.RegisterRetryAfter(TimeSpan.FromSeconds(5));
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(20));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            using var lease = await limiter.AcquireAsync(cancellation.Token);
        });
    }

    private static RiotApiClient CreateRiotClient(HttpClient client, RiotOptions options)
    {
        options.Validate();
        var timeProvider = TimeProvider.System;
        return new RiotApiClient(
            client,
            options,
            new RiotRateLimiter(options, timeProvider),
            timeProvider,
            new OperationalMetrics());
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, int, HttpResponseMessage> responseFactory;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
            : this((request, _) => responseFactory(request))
        {
        }

        public StubHttpMessageHandler(Func<HttpRequestMessage, int, HttpResponseMessage> responseFactory)
        {
            this.responseFactory = responseFactory;
        }

        public HttpRequestMessage? Request { get; private set; }

        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            CallCount++;
            return Task.FromResult(responseFactory(request, CallCount));
        }
    }
}
