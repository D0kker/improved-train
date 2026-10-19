using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LolAnalyzer.Application.Jobs;
using LolAnalyzer.Application.Riot;
using LolAnalyzer.Application.Stateless;
using LolAnalyzer.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace LolAnalyzer.IntegrationTests;

public sealed class StatelessEndpointTests
{
    [Fact]
    public async Task SearchReturnsFiveMatchesAndNoJobIsCreated()
    {
        await using var factory = new StatelessApiFactory();
        using var client = factory.CreateClient();
        var result = await client.GetFromJsonAsync<PlayerSearchResult>("/api/v1/players/by-riot-id/Test%20User/TEST/analysis", TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal(5, result.Matches.Items.Count);
        Assert.Equal(45, result.Summary.OtherPlayerParticipations);
        Assert.Null(factory.Services.GetService<AnalysisJobService>());
        Assert.DoesNotContain(factory.Services.GetServices<IHostedService>(), service => service.GetType().Name.Contains("AnalysisJob", StringComparison.Ordinal));
        using var ready = await client.GetAsync("/health/ready", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, ready.StatusCode);
    }

    [Fact]
    public async Task DirectPostWorksOnFreshApiWithoutLookupAndIsIndependentOfOtherRequests()
    {
        await using var factory = new StatelessApiFactory();
        using var client = factory.CreateClient();
        var first = await client.PostAsJsonAsync("/api/v1/matches/LA1_TARGET/analyze", new { ownerPuuid = "owner", platformRegion = "LA1" }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var result = await first.Content.ReadFromJsonAsync<MatchAnalysisResult>(TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal(9, result.Match.Familiarity!.KnownPlayers);
        Assert.Equal("complete", result.Coverage.Status);
        Assert.Equal(0, factory.Riot.AccountCalls);
        var other = await client.PostAsJsonAsync("/api/v1/matches/LA1_ALT/analyze", new { ownerPuuid = "alt-owner", platformRegion = "LA1" }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, other.StatusCode);
        var second = await other.Content.ReadFromJsonAsync<MatchAnalysisResult>(TestContext.Current.CancellationToken);
        Assert.All(second!.Encounters, encounter => Assert.StartsWith("alt-", encounter.OtherPlayerPuuid));
    }

    [Fact]
    public async Task SharedMatchesAcrossSimultaneousHttpRequestsRemainDeterministic()
    {
        await using var factory = new StatelessApiFactory();
        using var client = factory.CreateClient();
        var responses = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => client.PostAsJsonAsync(
            "/api/v1/matches/LA1_TARGET/analyze", new { ownerPuuid = "owner", platformRegion = "LA1" }, TestContext.Current.CancellationToken)));
        var bodies = await Task.WhenAll(responses.Select(response => response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)));
        Assert.All(responses, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));
        Assert.All(bodies, body => Assert.Equal(bodies[0], body));
    }

    [Fact]
    public async Task FailureCoverageIsVisibleEvenWhenHttpSucceeds()
    {
        await using var factory = new StatelessApiFactory();
        factory.Riot.FailingPuuid = "p1";
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/matches/LA1_TARGET/analyze", new { ownerPuuid = "owner" }, TestContext.Current.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<MatchAnalysisResult>(TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("partial", result!.Coverage.Status);
        Assert.NotEmpty(result.Coverage.Failures);
    }

    [Theory]
    [InlineData("/api/v1/players/owner/analysis", "POST")]
    [InlineData("/api/v1/jobs/00000000-0000-0000-0000-000000000001", "GET")]
    [InlineData("/api/v1/jobs/00000000-0000-0000-0000-000000000001/cancel", "POST")]
    [InlineData("/api/v1/players/owner/matches/sync", "POST")]
    [InlineData("/api/v1/players/owner/refresh-schedule", "PUT")]
    public async Task RetiredStatefulEndpointsReturnAnExplicitMigrationResponse(string path, string method)
    {
        await using var factory = new StatelessApiFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Gone, response.StatusCode);
        Assert.Contains("retired", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);
        Assert.Empty(factory.Riot.Details);
    }

    [Fact]
    public async Task CompatibilityReadsAlsoWorkWithoutPriorSearch()
    {
        await using var factory = new StatelessApiFactory();
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/v1/players/owner/summary", TestContext.Current.CancellationToken);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(5, json.RootElement.GetProperty("matchesAnalyzed").GetInt32());
    }

    [Theory]
    [InlineData("missing", "owner", HttpStatusCode.NotFound)]
    [InlineData("LA1_TARGET", "not-in-match", HttpStatusCode.BadRequest)]
    public async Task InvalidOrMissingMatchesDoNotLeakInternalErrors(string matchId, string owner, HttpStatusCode expected)
    {
        await using var factory = new StatelessApiFactory();
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync($"/api/v1/matches/{matchId}/analyze", new { ownerPuuid = owner }, TestContext.Current.CancellationToken);
        Assert.Equal(expected, response.StatusCode);
        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.Equal("nosniff", Assert.Single(response.Headers.GetValues("X-Content-Type-Options")));
    }

    [Fact]
    public async Task RequestBudgetReturnsAControlledTimeoutAndCancelsWork()
    {
        await using var factory = new StatelessApiFactory(timeoutSeconds: 5);
        factory.Riot.DelayMilliseconds = 6000;
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/players/by-riot-id/Test/TEST/analysis", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.GatewayTimeout, response.StatusCode);
        Assert.Empty(factory.Riot.Details);
    }

    [Fact]
    public async Task OversizedRequestsAreRejectedWithoutCallingRiot()
    {
        await using var factory = new StatelessApiFactory();
        using var client = factory.CreateClient();
        using var body = new StringContent(new string('x', 70_000), System.Text.Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/v1/matches/LA1_TARGET/analyze", body, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        Assert.Empty(factory.Riot.Details);
    }

    [Fact]
    public async Task ApiArtifactHasNoEfOrRedisRuntimeDependencies()
    {
        var deps = await File.ReadAllTextAsync(Path.ChangeExtension(typeof(Program).Assembly.Location, ".deps.json"), TestContext.Current.CancellationToken);
        Assert.DoesNotContain("EntityFrameworkCore", deps, StringComparison.Ordinal);
        Assert.DoesNotContain("StackExchange.Redis", deps, StringComparison.Ordinal);
    }
}

public sealed class StatelessApiFactory(int timeoutSeconds = 180) : WebApplicationFactory<Program>
{
    public StatelessFixture Riot { get; } = new();
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["RIOT_API_KEY"] = "simulated-test-key",
            ["RIOT_PLATFORM_REGION"] = "LA1",
            ["OnDemandAnalysis:RequestTimeoutSeconds"] = timeoutSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture)
        }));
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<OnDemandAnalysisOptions>();
            services.AddSingleton(new OnDemandAnalysisOptions { RequestTimeoutSeconds = timeoutSeconds });
            services.RemoveAll<IRiotApiClient>();
            services.AddSingleton<IRiotApiClient>(Riot);
        });
    }
}
