using System.Net;
using System.Text;
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
        var riotClient = new RiotApiClient(client, new RiotOptions
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
        var riotClient = new RiotApiClient(client, new RiotOptions { ApiKey = "unit-test-key" });

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
        var riotClient = new RiotApiClient(client, new RiotOptions { ApiKey = "unit-test-key" });

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
        var riotClient = new RiotApiClient(client, new RiotOptions { ApiKey = "unit-test-key" });

        var match = await riotClient.GetMatchAsync("TEST_1", TestContext.Current.CancellationToken);

        Assert.NotNull(match);
        Assert.Equal("TEST_1", match.RiotMatchId);
        Assert.Equal(responseBody, match.RawJson);
        Assert.Equal(55, Assert.Single(match.Participants).Cs);
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(responseFactory(request));
        }
    }
}
