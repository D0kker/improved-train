using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using LolAnalyzer.Application.Riot;
using LolAnalyzer.Infrastructure.Riot;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace LolAnalyzer.IntegrationTests;

public sealed class RankedEndpointTests
{
    [Fact]
    public async Task EndpointReturnsOnlyTheOwnersClassificationWithAdmissibleJsonAndCachedObservationTime()
    {
        await using var factory = new Factory();
        using var client = factory.CreateClient();
        var first = await client.GetAsync("/api/v1/players/owner/ranked?platformRegion=EUW1", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        using var json = JsonDocument.Parse(await first.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var entry = json.RootElement.GetProperty("entries")[0];
        Assert.Equal("GOLD", entry.GetProperty("tier").GetString());
        Assert.Equal("II", entry.GetProperty("division").GetString());
        Assert.Equal(54, entry.GetProperty("leaguePoints").GetInt32());
        Assert.True(json.RootElement.TryGetProperty("observedAt", out _));
        var second = await client.GetFromJsonAsync<RiotRankedProfile>("/api/v1/players/owner/ranked?platformRegion=euw1", TestContext.Current.CancellationToken);
        Assert.NotNull(second);
        Assert.Equal(1, factory.Handler.Calls);
        Assert.Equal("euw1.api.riotgames.com", factory.Handler.Host);
        Assert.Equal("/lol/league/v4/entries/by-puuid/owner", factory.Handler.Path);
    }

    [Fact]
    public async Task RequestLoggingPreservesSafeTelemetryWithoutLoggingPuuidOrUpstreamCredentials()
    {
        await using var factory = new Factory();
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/v1/players/private-owner-puuid/ranked", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(factory.Logs.Messages, message => message.Contains("HTTP request completed", StringComparison.Ordinal));
        Assert.DoesNotContain(factory.Logs.Messages, message => message.Contains("private-owner-puuid", StringComparison.Ordinal)
            || message.Contains("synthetic-test-key", StringComparison.Ordinal)
            || message.Contains("api.riotgames.com", StringComparison.Ordinal));
    }

    [Fact]
    public async Task OnlySuccessfulEmptyJsonIndicatesUnranked()
    {
        await using var factory = new Factory(payload: "[]");
        using var client = factory.CreateClient();
        var profile = await client.GetFromJsonAsync<RiotRankedProfile>("/api/v1/players/owner/ranked", TestContext.Current.CancellationToken);
        Assert.NotNull(profile);
        Assert.Empty(profile.Entries);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound, HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.TooManyRequests, HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError, HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.BadGateway, HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.Forbidden, HttpStatusCode.BadGateway)]
    public async Task ErrorsRemainUnavailableAndDoNotLeakUpstreamData(HttpStatusCode upstream, HttpStatusCode expected)
    {
        await using var factory = new Factory(status: upstream, payload: "sensitive-upstream-message");
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/v1/players/owner/ranked", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Equal(expected, response.StatusCode);
        Assert.DoesNotContain("sensitive-upstream", body, StringComparison.Ordinal);
        Assert.DoesNotContain("entries", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SecondaryDeadlineCancelsItsWorkAndDoesNotAffectHealth()
    {
        await using var factory = new Factory(delay: true);
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/v1/players/owner/ranked", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.GatewayTimeout, response.StatusCode);
        await factory.Handler.Cancelled.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        using var ready = await client.GetAsync("/health/ready", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, ready.StatusCode);
    }

    [Fact]
    public async Task CallerCancellationCancelsOnlyItsSecondaryWork()
    {
        await using var factory = new Factory(delay: true);
        using var client = factory.CreateClient();
        using var cancellation = new CancellationTokenSource();
        var request = client.GetAsync("/api/v1/players/owner/ranked", cancellation.Token);
        await factory.Handler.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => request);
        await factory.Handler.Cancelled.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        using var ready = await client.GetAsync("/health/ready", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, ready.StatusCode);
    }

    [Theory]
    [InlineData("PH2")]
    [InlineData("TH2")]
    [InlineData("ME1")]
    [InlineData("not-a-platform")]
    public async Task UnsupportedPlatformReturnsAnErrorWithoutAnyRiotLookup(string platform)
    {
        await using var factory = new Factory();
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/v1/players/owner/ranked?platformRegion=" + platform, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, factory.Handler.Calls);
    }

    private sealed class Factory(HttpStatusCode status = HttpStatusCode.OK, string payload = """[{"queueType":"RANKED_SOLO_5x5","tier":"GOLD","rank":"II","leaguePoints":54}]""", bool delay = false) : WebApplicationFactory<Program>
    {
        public Handler Handler { get; } = new(status, payload, delay);
        public CaptureLogs Logs { get; } = new();
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureLogging(logging => logging.AddProvider(Logs)
                .AddFilter("System.Net.Http.HttpClient", LogLevel.Trace));
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RIOT_API_KEY"] = "synthetic-test-key",
                ["RIOT_PLATFORM_REGION"] = "LA1",
                ["Riot:MaxRetryAttempts"] = "0",
                ["Riot:RankedRequestTimeoutSeconds"] = "1",
            }));
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<RiotOptions>();
                services.AddSingleton(new RiotOptions
                {
                    ApiKey = "synthetic-test-key",
                    PlatformRegion = "la1",
                    MaxRetryAttempts = 0,
                    RankedRequestTimeoutSeconds = 1,
                });
                services.AddHttpClient<IRiotApiClient, RiotApiClient>()
                    .ConfigurePrimaryHttpMessageHandler(() => Handler);
            });
        }
    }

    private sealed class CaptureLogs : ILoggerProvider
    {
        public ConcurrentQueue<string> Messages { get; } = new();
        public ILogger CreateLogger(string categoryName) => new CaptureLogger(Messages);
        public void Dispose() { }

        private sealed class CaptureLogger(ConcurrentQueue<string> messages) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
                Func<TState, Exception?, string> formatter) => messages.Enqueue(formatter(state, exception));
        }
    }

    private sealed class Handler(HttpStatusCode status, string payload, bool delay) : HttpMessageHandler
    {
        public int Calls { get; private set; }
        public string? Host { get; private set; }
        public string? Path { get; private set; }
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Cancelled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Calls++;
            Host = request.RequestUri!.Host;
            Path = request.RequestUri.AbsolutePath;
            Entered.TrySetResult();
            if (delay)
            {
                try { await Task.Delay(Timeout.Infinite, token); }
                catch (OperationCanceledException) { Cancelled.TrySetResult(); throw; }
            }
            return new HttpResponseMessage(status) { Content = new StringContent(payload, Encoding.UTF8, "application/json") };
        }
    }
}
