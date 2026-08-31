using System.Net;
using System.Text.Json;
using LolAnalyzer.Application.Analysis;
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
        Assert.Equal(
            0,
            body.RootElement.GetProperty("relationshipAnalysis").GetProperty("relationshipsRebuilt").GetInt32());
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

    [Fact]
    public async Task SprintThreeReadEndpointsReturnAnalysisContractsWithoutCallingRiot()
    {
        using var client = _factory.CreateClient();

        var summary = await client.GetAsync(
            "/api/v1/players/test-owner-puuid/summary",
            TestContext.Current.CancellationToken);
        var matches = await client.GetAsync(
            "/api/v1/players/test-owner-puuid/matches?page=1&pageSize=20",
            TestContext.Current.CancellationToken);
        var detail = await client.GetAsync(
            "/api/v1/matches/TEST_1",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, summary.StatusCode);
        Assert.Equal(HttpStatusCode.OK, matches.StatusCode);
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
    }

    [Fact]
    public async Task MatchHistoryRejectsPaginationThatCouldOverflowTheRepositoryOffset()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(
            "/api/v1/players/test-owner-puuid/matches?page=2147483647&pageSize=100",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RelationshipsExposeEvidenceAndPrudentPremadeLabelWithoutRiot()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(
            "/api/v1/players/test-owner-puuid/relationships?page=1&pageSize=20&minimumConfidence=HIGH",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken));
        var relationship = body.RootElement.GetProperty("items")[0];
        Assert.Equal("HIGH", relationship.GetProperty("relationshipConfidence").GetString());
        Assert.Equal("likely premade", relationship.GetProperty("premadeLabel").GetString());
        Assert.Equal(0.8m, relationship.GetProperty("sameTeamRatio").GetDecimal());
    }

    [Fact]
    public async Task RelationshipsRejectUnknownConfidenceLabels()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(
            "/api/v1/players/test-owner-puuid/relationships?minimumConfidence=VERIFIED",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RelationshipsReturnNotFoundAndRejectUnboundedPagination()
    {
        using var client = _factory.CreateClient();

        var notFound = await client.GetAsync(
            "/api/v1/players/unknown/relationships",
            TestContext.Current.CancellationToken);
        var invalidPageSize = await client.GetAsync(
            "/api/v1/players/test-owner-puuid/relationships?pageSize=101",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, notFound.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalidPageSize.StatusCode);
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
            services.RemoveAll<IPlayerAnalysisRepository>();
            services.RemoveAll<IPlayerRelationshipRepository>();
            services.AddSingleton<IRiotApiClient>(new SimulatedRiotApiClient());
            services.AddSingleton<IPlayerRepository>(new InMemoryPlayerRepository());
            services.AddSingleton<IMatchRepository>(new InMemoryMatchRepository());
            services.AddSingleton<IPlayerAnalysisRepository>(new InMemoryPlayerAnalysisRepository());
            services.AddSingleton<IPlayerRelationshipRepository>(new InMemoryPlayerRelationshipRepository());
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

internal sealed class InMemoryPlayerAnalysisRepository : IPlayerAnalysisRepository
{
    private static readonly Guid OwnerId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public Task<PlayerAnalysisInput?> LoadInputAsync(string ownerPuuid, CancellationToken cancellationToken) =>
        Task.FromResult<PlayerAnalysisInput?>(ownerPuuid == "test-owner-puuid"
            ? new PlayerAnalysisInput(OwnerId, [])
            : null);

    public Task<MatchFamiliarityInput?> LoadFamiliarityInputAsync(
        string ownerPuuid,
        string targetRiotMatchId,
        CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task ReplaceEncountersAsync(
        Guid ownerPlayerId,
        IReadOnlyCollection<PlayerEncounterAggregate> encounters,
        CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<PlayerSummary?> GetSummaryAsync(string puuid, CancellationToken cancellationToken) =>
        Task.FromResult<PlayerSummary?>(puuid == "test-owner-puuid"
            ? new PlayerSummary(puuid, "Ana", "LAN", 1, 0, 1, 0, 9, 0)
            : null);

    public Task<IReadOnlyList<PlayerEncounterView>?> GetRepeatedPlayersAsync(
        string puuid,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<PlayerEncounterView>?>(puuid == "test-owner-puuid" ? [] : null);

    public Task<PagedPlayerMatches?> GetMatchesAsync(
        string puuid,
        int page,
        int pageSize,
        int? queueId,
        CancellationToken cancellationToken) =>
        Task.FromResult<PagedPlayerMatches?>(puuid == "test-owner-puuid"
            ? new PagedPlayerMatches(
                page,
                pageSize,
                1,
                [new PlayerMatchListItem("TEST_1", 420, DateTimeOffset.UnixEpoch, 1200, "Annie", 0, 0, 0, false)])
            : null);

    public Task<MatchDetail?> GetMatchDetailAsync(string riotMatchId, CancellationToken cancellationToken) =>
        Task.FromResult<MatchDetail?>(riotMatchId == "TEST_1"
            ? new MatchDetail("TEST_1", 420, DateTimeOffset.UnixEpoch, 1200, [])
            : null);
}

internal sealed class InMemoryPlayerRelationshipRepository : IPlayerRelationshipRepository
{
    public Task<IReadOnlyList<RelationshipMatchSnapshot>> LoadMatchesAsync(
        int batchSize,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<RelationshipMatchSnapshot>>([]);

    public Task ReplaceRelationshipsAsync(
        IReadOnlyCollection<PlayerRelationshipAggregate> relationships,
        int batchSize,
        CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<PagedPlayerRelationshipQuery?> GetRelationshipsAsync(
        string puuid,
        int page,
        int pageSize,
        RelationshipConfidence minimumConfidence,
        CancellationToken cancellationToken) =>
        Task.FromResult<PagedPlayerRelationshipQuery?>(puuid == "test-owner-puuid"
            ? new PagedPlayerRelationshipQuery(
                page,
                pageSize,
                minimumConfidence > RelationshipConfidence.High ? 0 : 1,
                minimumConfidence > RelationshipConfidence.High ? [] : [new PlayerRelationshipQueryItem(
                    "other-puuid",
                    "Bea",
                    "LAN",
                    5,
                    4,
                    1,
                    4,
                    3,
                    DateTimeOffset.UnixEpoch,
                    DateTimeOffset.UnixEpoch.AddDays(4),
                    60,
                    RelationshipConfidence.High)])
            : null);
}
