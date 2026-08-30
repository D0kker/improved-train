using System.Net;
using System.Text.Json;
using LolAnalyzer.Application.Matches;
using LolAnalyzer.Application.Players;
using LolAnalyzer.Application.Riot;
using LolAnalyzer.Domain.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace LolAnalyzer.IntegrationTests;

public sealed class PlayerLookupEndpointTests : IClassFixture<LolAnalyzerApiFactory>
{
    private readonly LolAnalyzerApiFactory _factory;

    public PlayerLookupEndpointTests(LolAnalyzerApiFactory factory) => _factory = factory;

    [Fact]
    public async Task LookupReturnsThePuuidResolvedByTheSimulatedRiotClient()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/players/by-riot-id/Ana/LAN", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken));
        Assert.Equal("simulated-puuid", body.RootElement.GetProperty("puuid").GetString());
        Assert.Equal("Ana", body.RootElement.GetProperty("gameName").GetString());
    }

    [Fact]
    public async Task LivenessDoesNotDependOnPostgresqlRedisOrRiot()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SynchronizationEndpointExecutesTheLocalFirstVerticalSliceWithSimulatedServices()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsync(
            "/api/v1/players/test-owner-puuid/matches/sync?count=1",
            content: null,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken));
        Assert.Equal(1, body.RootElement.GetProperty("downloaded").GetInt32());
        Assert.Equal(1, body.RootElement.GetProperty("persisted").GetInt32());
    }

    [Fact]
    public async Task SynchronizationEndpointRejectsAnUnboundedCount()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsync(
            "/api/v1/players/test-owner-puuid/matches/sync?count=21",
            content: null,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

public sealed class LolAnalyzerApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:ApplyMigrations"] = "false",
                ["RIOT_API_KEY"] = "unit-test-key",
            }));
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IRiotApiClient>();
            services.RemoveAll<IPlayerRepository>();
            services.RemoveAll<IMatchRepository>();
            services.AddSingleton<IRiotApiClient>(new SimulatedRiotApiClient());
            services.AddSingleton<IPlayerRepository>(new InMemoryPlayerRepository());
            services.AddSingleton<IMatchRepository>(new InMemoryMatchRepository());
        });
    }
}

internal sealed class SimulatedRiotApiClient : IRiotApiClient
{
    private static readonly string[] MatchIds = ["TEST_1"];

    public Task<RiotAccount?> GetAccountByRiotIdAsync(string gameName, string tagLine, CancellationToken cancellationToken) =>
        Task.FromResult<RiotAccount?>(new RiotAccount("simulated-puuid", gameName, tagLine));

    public Task<IReadOnlyList<string>> GetMatchIdsAsync(string puuid, int start, int count, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<string>>(MatchIds);

    public Task<RiotMatchData?> GetMatchAsync(string riotMatchId, CancellationToken cancellationToken) =>
        Task.FromResult<RiotMatchData?>(new RiotMatchData(
            riotMatchId,
            420,
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch.AddMinutes(20),
            1200,
            "test",
            "{}",
            [new RiotMatchParticipantData("simulated-puuid", "Ana", "LAN", 100, 1, 1, "Annie", "MIDDLE", "MIDDLE", 0, 0, 0, false, 0, 0, 0, 0)]));
}

internal sealed class InMemoryPlayerRepository : IPlayerRepository
{
    public Task<Player> UpsertAsync(
        string puuid,
        string gameName,
        string tagLine,
        string platformRegion,
        CancellationToken cancellationToken) =>
        Task.FromResult(new Player
        {
            Puuid = puuid,
            GameName = gameName,
            TagLine = tagLine,
            PlatformRegion = platformRegion,
        });
}

internal sealed class InMemoryMatchRepository : IMatchRepository
{
    private readonly HashSet<string> _savedMatchIds = new(StringComparer.Ordinal);

    public Task<IReadOnlySet<string>> FindExistingRiotMatchIdsAsync(
        IReadOnlyCollection<string> riotMatchIds,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlySet<string>>(new HashSet<string>(_savedMatchIds, StringComparer.Ordinal));

    public Task<bool> SaveIfMissingAsync(RiotMatchData match, string platformRegion, CancellationToken cancellationToken)
    {
        return Task.FromResult(_savedMatchIds.Add(match.RiotMatchId));
    }
}
