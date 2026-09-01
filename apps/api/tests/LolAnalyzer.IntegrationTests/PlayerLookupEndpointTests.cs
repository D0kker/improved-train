using System.Net;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Net.Http.Json;
using LolAnalyzer.Application.Analysis;
using LolAnalyzer.Application.Jobs;
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
    public async Task LookupReturnsTheExistingLocalPlayerBeforeRiot()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/players/by-riot-id/Ana/LAN", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken));
        Assert.Equal("test-owner-puuid", body.RootElement.GetProperty("puuid").GetString());
        Assert.Equal("Ana", body.RootElement.GetProperty("gameName").GetString());
    }

    [Fact]
    public async Task LookupResolvesAnUnknownRiotIdWithTheSimulatedClient()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/players/by-riot-id/Nueva/LAN", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken));
        Assert.Equal("simulated-puuid", body.RootElement.GetProperty("puuid").GetString());
        Assert.Equal("Nueva", body.RootElement.GetProperty("gameName").GetString());
    }

    [Fact]
    public async Task LivenessDoesNotDependOnPostgresqlRedisOrRiot()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Equal("same-origin", response.Headers.GetValues("Referrer-Policy").Single());
        Assert.Equal(
            "camera=(), geolocation=(), microphone=()",
            response.Headers.GetValues("Permissions-Policy").Single());
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
    public async Task AnalysisJobStartPersistsQueuedStateAndStatusEndpointReturnsIt()
    {
        using var client = _factory.CreateClient();

        var start = await client.PostAsJsonAsync(
            "/api/v1/players/test-owner-puuid/analysis",
            new { matchCount = 70 },
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Accepted, start.StatusCode);
        using var startedBody = JsonDocument.Parse(
            await start.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken));
        var jobId = startedBody.RootElement.GetProperty("jobId").GetGuid();
        Assert.Equal("queued", startedBody.RootElement.GetProperty("status").GetString());
        Assert.Equal(70, startedBody.RootElement.GetProperty("matchesRequested").GetInt32());
        Assert.Equal(0, startedBody.RootElement.GetProperty("matchesProcessed").GetInt32());
        Assert.Equal($"/api/v1/jobs/{jobId}", start.Headers.Location?.OriginalString);

        var status = await client.GetAsync(
            $"/api/v1/jobs/{jobId}",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, status.StatusCode);
        using var statusBody = JsonDocument.Parse(
            await status.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken));
        Assert.Equal(jobId, statusBody.RootElement.GetProperty("jobId").GetGuid());
        Assert.Equal("test-owner-puuid", statusBody.RootElement.GetProperty("puuid").GetString());
        Assert.Equal(JsonValueKind.Null, statusBody.RootElement.GetProperty("errorCode").ValueKind);

        var cancel = await client.PostAsync(
            $"/api/v1/jobs/{jobId}/cancel",
            content: null,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);
        using var cancelledBody = JsonDocument.Parse(
            await cancel.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken));
        Assert.Equal("cancelled", cancelledBody.RootElement.GetProperty("status").GetString());
        Assert.NotEqual(JsonValueKind.Null, cancelledBody.RootElement.GetProperty("completedAt").ValueKind);
    }

    [Fact]
    public async Task AnalysisJobEndpointsRejectInvalidCountsAndUnknownJobs()
    {
        using var client = _factory.CreateClient();

        var invalid = await client.PostAsJsonAsync(
            "/api/v1/players/test-owner-puuid/analysis",
            new { matchCount = 201 },
            cancellationToken: TestContext.Current.CancellationToken);
        var unknown = await client.GetAsync(
            $"/api/v1/jobs/{Guid.NewGuid()}",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
    }

    [Fact]
    public async Task RefreshScheduleRequiresExplicitOptInAndCanBeDisabled()
    {
        using var client = _factory.CreateClient();

        var enabled = await client.PutAsJsonAsync(
            "/api/v1/players/test-owner-puuid/refresh-schedule",
            new { enabled = true, intervalMinutes = 60, matchCount = 100 },
            TestContext.Current.CancellationToken);
        var read = await client.GetAsync(
            "/api/v1/players/test-owner-puuid/refresh-schedule",
            TestContext.Current.CancellationToken);
        var disabled = await client.PutAsJsonAsync(
            "/api/v1/players/test-owner-puuid/refresh-schedule",
            new { enabled = false, intervalMinutes = 60, matchCount = 100 },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, enabled.StatusCode);
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        Assert.Equal(HttpStatusCode.OK, disabled.StatusCode);
        using var disabledBody = JsonDocument.Parse(
            await disabled.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken));
        Assert.False(disabledBody.RootElement.GetProperty("enabled").GetBoolean());
    }

    [Fact]
    public async Task RefreshScheduleRejectsUnsafeFrequencyAndUnknownPlayer()
    {
        using var client = _factory.CreateClient();

        var invalid = await client.PutAsJsonAsync(
            "/api/v1/players/test-owner-puuid/refresh-schedule",
            new { enabled = true, intervalMinutes = 14, matchCount = 20 },
            TestContext.Current.CancellationToken);
        var unknown = await client.PutAsJsonAsync(
            "/api/v1/players/unknown/refresh-schedule",
            new { enabled = true, intervalMinutes = 60, matchCount = 20 },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
    }

    [Fact]
    public async Task EquivalentActiveAnalysisRequestsReturnTheSamePersistentJob()
    {
        using var client = _factory.CreateClient();
        var puuid = $"duplicate-{Guid.NewGuid():N}";

        var first = await client.PostAsJsonAsync(
            $"/api/v1/players/{puuid}/analysis",
            new { matchCount = 40 },
            TestContext.Current.CancellationToken);
        var second = await client.PostAsJsonAsync(
            $"/api/v1/players/{puuid}/analysis",
            new { matchCount = 40 },
            TestContext.Current.CancellationToken);

        using var firstBody = JsonDocument.Parse(
            await first.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken));
        using var secondBody = JsonDocument.Parse(
            await second.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken));
        Assert.Equal(
            firstBody.RootElement.GetProperty("jobId").GetGuid(),
            secondBody.RootElement.GetProperty("jobId").GetGuid());
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
    public async Task MatchDetailExposesDistinctPrudentPremadeGroups()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(
            "/api/v1/matches/TEST_1",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var body = JsonDocument.Parse(json);
        var groups = body.RootElement.GetProperty("premadeGroups");
        Assert.Equal(2, groups.GetArrayLength());
        Assert.Equal(1, groups[0].GetProperty("groupNumber").GetInt32());
        Assert.Equal("possible premade · high evidence", groups[0].GetProperty("label").GetString());
        Assert.Equal(3, groups[0].GetProperty("members").GetArrayLength());
        Assert.Equal(200, groups[1].GetProperty("teamId").GetInt32());
        Assert.Equal(2, groups[1].GetProperty("members").GetArrayLength());
        Assert.DoesNotContain("verified", json);
    }

    [Fact]
    public async Task MatchDetailExposesFamiliarityOnlyWithOwnerContext()
    {
        using var client = _factory.CreateClient();

        var contextual = await client.GetAsync(
            "/api/v1/matches/TEST_1?ownerPuuid=test-owner-puuid",
            TestContext.Current.CancellationToken);
        var withoutOwner = await client.GetAsync(
            "/api/v1/matches/TEST_1",
            TestContext.Current.CancellationToken);

        using var contextualBody = JsonDocument.Parse(
            await contextual.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken));
        using var plainBody = JsonDocument.Parse(
            await withoutOwner.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken));
        var familiarity = contextualBody.RootElement.GetProperty("familiarity");
        Assert.Equal(1, familiarity.GetProperty("knownPlayers").GetInt32());
        Assert.Equal(50m, familiarity.GetProperty("familiarityPercentage").GetDecimal());
        Assert.Equal("Available", familiarity.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, plainBody.RootElement.GetProperty("familiarity").ValueKind);
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

    [Fact]
    public async Task NetworkExposesBoundedDepthOneGraphAndTruncationMetadata()
    {
        using var client = _factory.CreateClient();

        var completeResponse = await client.GetAsync(
            "/api/v1/players/test-owner-puuid/network?maxNodes=2&maxEdges=1&minimumConfidence=HIGH",
            TestContext.Current.CancellationToken);
        var truncatedResponse = await client.GetAsync(
            "/api/v1/players/test-owner-puuid/network?maxNodes=1&maxEdges=1&minimumConfidence=HIGH",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);
        using var completeBody = JsonDocument.Parse(await completeResponse.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken));
        var edge = completeBody.RootElement.GetProperty("edges")[0];
        Assert.Equal("test-owner-puuid", edge.GetProperty("sourcePuuid").GetString());
        Assert.Equal("other-puuid", edge.GetProperty("targetPuuid").GetString());
        Assert.Equal("likely premade", edge.GetProperty("premadeLabel").GetString());

        Assert.Equal(HttpStatusCode.OK, truncatedResponse.StatusCode);
        using var body = JsonDocument.Parse(await truncatedResponse.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken));
        Assert.Equal("test-owner-puuid", body.RootElement.GetProperty("center").GetProperty("puuid").GetString());
        Assert.Equal(1, body.RootElement.GetProperty("nodes").GetArrayLength());
        Assert.Equal(0, body.RootElement.GetProperty("edges").GetArrayLength());
        var metadata = body.RootElement.GetProperty("metadata");
        Assert.Equal(1, metadata.GetProperty("depth").GetInt32());
        Assert.True(metadata.GetProperty("truncated").GetBoolean());
        Assert.Equal(2, metadata.GetProperty("totalAvailableNodes").GetInt32());
        Assert.Equal(1, metadata.GetProperty("totalAvailableEdges").GetInt32());
    }

    [Fact]
    public async Task NetworkSupportsStrengthFiltersAndEmptyGraphs()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(
            "/api/v1/players/test-owner-puuid/network?minimumScore=61",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken));
        Assert.Equal(1, body.RootElement.GetProperty("nodes").GetArrayLength());
        Assert.Equal(0, body.RootElement.GetProperty("edges").GetArrayLength());
        Assert.False(body.RootElement.GetProperty("metadata").GetProperty("truncated").GetBoolean());
    }

    [Fact]
    public async Task NetworkRejectsInvalidLimitsAndReturnsNotFound()
    {
        using var client = _factory.CreateClient();

        var invalid = await client.GetAsync(
            "/api/v1/players/test-owner-puuid/network?maxNodes=51",
            TestContext.Current.CancellationToken);
        var notFound = await client.GetAsync(
            "/api/v1/players/unknown/network",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, notFound.StatusCode);
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
            services.RemoveAll<IAnalysisJobRepository>();
            services.RemoveAll<IPlayerRefreshScheduleRepository>();
            services.AddSingleton<IRiotApiClient>(new SimulatedRiotApiClient());
            services.AddSingleton<IPlayerRepository>(new InMemoryPlayerRepository());
            services.AddSingleton<IMatchRepository>(new InMemoryMatchRepository());
            services.AddSingleton<IPlayerAnalysisRepository>(new InMemoryPlayerAnalysisRepository());
            services.AddSingleton<IPlayerRelationshipRepository>(new InMemoryPlayerRelationshipRepository());
            services.AddSingleton<IAnalysisJobRepository>(new InMemoryAnalysisJobRepository());
            services.AddSingleton<IPlayerRefreshScheduleRepository>(new InMemoryRefreshScheduleRepository());
        });
    }
}

internal sealed class InMemoryRefreshScheduleRepository : IPlayerRefreshScheduleRepository
{
    private readonly ConcurrentDictionary<string, PlayerRefreshSchedule> schedules = new();

    public Task<PlayerRefreshSchedule?> FindAsync(string puuid, CancellationToken cancellationToken)
    {
        schedules.TryGetValue(puuid, out var schedule);
        return Task.FromResult(schedule);
    }

    public Task<PlayerRefreshSchedule?> UpsertAsync(
        string puuid,
        int requestedCount,
        int intervalMinutes,
        bool enabled,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (puuid != "test-owner-puuid")
        {
            return Task.FromResult<PlayerRefreshSchedule?>(null);
        }

        var schedule = new PlayerRefreshSchedule
        {
            Puuid = puuid,
            RequestedCount = requestedCount,
            IntervalMinutes = intervalMinutes,
            Enabled = enabled,
            NextRunAt = now.AddMinutes(intervalMinutes),
            CreatedAt = now,
            UpdatedAt = now,
        };
        schedules[puuid] = schedule;
        return Task.FromResult<PlayerRefreshSchedule?>(schedule);
    }

    public Task<PlayerRefreshSchedule?> ClaimNextDueAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken) => Task.FromResult<PlayerRefreshSchedule?>(null);
}

internal sealed class InMemoryAnalysisJobRepository : IAnalysisJobRepository
{
    private readonly ConcurrentDictionary<Guid, AnalysisJob> _jobs = new();
    private readonly object _gate = new();

    public Task<AnalysisJob> AddOrGetActiveAsync(AnalysisJob job, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var active = _jobs.Values.FirstOrDefault(candidate =>
                candidate.Puuid == job.Puuid
                && candidate.RequestedCount == job.RequestedCount
                && candidate.Status is AnalysisJobStatus.Queued or AnalysisJobStatus.Running);
            if (active is not null)
            {
                return Task.FromResult(active);
            }

            if (!_jobs.TryAdd(job.Id, job))
            {
                throw new InvalidOperationException("The analysis job already exists.");
            }

            return Task.FromResult(job);
        }
    }

    public Task<AnalysisJob?> FindAsync(Guid jobId, CancellationToken cancellationToken)
    {
        _jobs.TryGetValue(jobId, out var job);
        return Task.FromResult(job);
    }

    public Task<AnalysisJob?> ClaimNextAsync(
        DateTimeOffset now,
        DateTimeOffset staleBefore,
        CancellationToken cancellationToken)
    {
        var job = _jobs.Values
            .Where(candidate => candidate.Status == AnalysisJobStatus.Queued
                || (candidate.Status == AnalysisJobStatus.Running && candidate.UpdatedAt < staleBefore))
            .OrderBy(candidate => candidate.CreatedAt)
            .FirstOrDefault();
        if (job is not null)
        {
            job.Status = AnalysisJobStatus.Running;
            job.StartedAt ??= now;
            job.UpdatedAt = now;
        }

        return Task.FromResult(job);
    }

    public Task UpdateProgressAsync(
        Guid jobId,
        int matchesProcessed,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (_jobs.TryGetValue(jobId, out var job) && job.Status == AnalysisJobStatus.Running)
        {
            job.MatchesProcessed = matchesProcessed;
            job.UpdatedAt = now;
        }

        return Task.CompletedTask;
    }

    public Task CompleteAsync(
        Guid jobId,
        int matchesProcessed,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (_jobs.TryGetValue(jobId, out var job) && job.Status == AnalysisJobStatus.Running)
        {
            job.Status = AnalysisJobStatus.Completed;
            job.MatchesProcessed = matchesProcessed;
            job.UpdatedAt = now;
            job.CompletedAt = now;
        }

        return Task.CompletedTask;
    }

    public Task FailAsync(
        Guid jobId,
        string errorCode,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (_jobs.TryGetValue(jobId, out var job) && job.Status == AnalysisJobStatus.Running)
        {
            job.Status = AnalysisJobStatus.Failed;
            job.ErrorCode = errorCode;
            job.UpdatedAt = now;
            job.CompletedAt = now;
        }

        return Task.CompletedTask;
    }

    public Task RequeueAsync(
        Guid jobId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (_jobs.TryGetValue(jobId, out var job) && job.Status == AnalysisJobStatus.Running)
        {
            job.Status = AnalysisJobStatus.Queued;
            job.UpdatedAt = now;
        }

        return Task.CompletedTask;
    }

    public Task<AnalysisJob?> CancelAsync(
        Guid jobId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (_jobs.TryGetValue(jobId, out var job)
            && job.Status is AnalysisJobStatus.Queued or AnalysisJobStatus.Running)
        {
            job.Status = AnalysisJobStatus.Cancelled;
            job.UpdatedAt = now;
            job.CompletedAt = now;
        }

        return Task.FromResult(job);
    }

    public Task<bool> IsCancelledAsync(Guid jobId, CancellationToken cancellationToken) =>
        Task.FromResult(_jobs.TryGetValue(jobId, out var job) && job.Status == AnalysisJobStatus.Cancelled);
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
    public Task<Player?> FindByRiotIdAsync(
        string gameName,
        string tagLine,
        CancellationToken cancellationToken) =>
        Task.FromResult<Player?>(gameName == "Ana" && tagLine == "LAN"
            ? new Player
            {
                Puuid = "test-owner-puuid",
                GameName = gameName,
                TagLine = tagLine,
                PlatformRegion = "la1",
            }
            : null);

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
        CancellationToken cancellationToken) =>
        Task.FromResult<MatchFamiliarityInput?>(
            ownerPuuid == "test-owner-puuid" && targetRiotMatchId == "TEST_1"
                ? new MatchFamiliarityInput(
                    OwnerId,
                    targetRiotMatchId,
                    [
                        new FamiliarityMatch(
                            "TEST_0",
                            DateTimeOffset.UnixEpoch,
                            [
                                new FamiliarityParticipant(OwnerId),
                                new FamiliarityParticipant(Guid.Parse("00000000-0000-0000-0000-000000000002")),
                            ]),
                        new FamiliarityMatch(
                            "TEST_1",
                            DateTimeOffset.UnixEpoch.AddMinutes(20),
                            [
                                new FamiliarityParticipant(OwnerId),
                                new FamiliarityParticipant(Guid.Parse("00000000-0000-0000-0000-000000000002")),
                                new FamiliarityParticipant(Guid.Parse("00000000-0000-0000-0000-000000000003")),
                            ]),
                    ])
                : null);

    public Task ReplaceEncountersAsync(
        Guid ownerPlayerId,
        IReadOnlyCollection<PlayerEncounterAggregate> encounters,
        CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<PlayerSummary?> GetSummaryAsync(string puuid, CancellationToken cancellationToken) =>
        Task.FromResult<PlayerSummary?>(puuid == "test-owner-puuid"
            ? new PlayerSummary(puuid, "Ana", "LAN", 1, 0, 1, 0, 9, 0, DateTimeOffset.UnixEpoch)
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
                [new PlayerMatchListItem("TEST_1", 420, DateTimeOffset.UnixEpoch, 1200, 1, "Annie", 0, 0, 0, false)])
            : null);

    public Task<MatchDetail?> GetMatchDetailAsync(string riotMatchId, CancellationToken cancellationToken) =>
        Task.FromResult<MatchDetail?>(riotMatchId == "TEST_1"
            ? new MatchDetail("TEST_1", 420, DateTimeOffset.UnixEpoch, 1200, [])
            : null);
}

internal sealed class InMemoryPlayerRelationshipRepository : IPlayerRelationshipRepository
{
    private static readonly Guid A = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid B = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private static readonly Guid C = Guid.Parse("00000000-0000-0000-0000-000000000003");
    private static readonly Guid D = Guid.Parse("00000000-0000-0000-0000-000000000004");
    private static readonly Guid E = Guid.Parse("00000000-0000-0000-0000-000000000005");
    private static readonly Guid F = Guid.Parse("00000000-0000-0000-0000-000000000006");

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
        int minimumScore,
        CancellationToken cancellationToken) =>
        Task.FromResult<PagedPlayerRelationshipQuery?>(puuid == "test-owner-puuid"
            ? new PagedPlayerRelationshipQuery(
                "test-owner-puuid",
                "Ana",
                "LAN",
                page,
                pageSize,
                minimumConfidence > RelationshipConfidence.High || minimumScore > 60 ? 0 : 1,
                minimumConfidence > RelationshipConfidence.High || minimumScore > 60 ? [] : [new PlayerRelationshipQueryItem(
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

    public Task<MatchPremadeGroupInput?> LoadMatchPremadeGroupInputAsync(
        string riotMatchId,
        CancellationToken cancellationToken) =>
        Task.FromResult<MatchPremadeGroupInput?>(riotMatchId == "TEST_1"
            ? new MatchPremadeGroupInput(
                [
                    Participant(A, "a", 100), Participant(B, "b", 100), Participant(C, "c", 100),
                    Participant(D, "d", 200), Participant(E, "e", 200), Participant(F, "", 200),
                ],
                [
                    Relationship(A, B, RelationshipConfidence.High),
                    Relationship(A, C, RelationshipConfidence.High),
                    Relationship(B, C, RelationshipConfidence.High),
                    Relationship(D, E, RelationshipConfidence.Medium),
                    Relationship(D, F, RelationshipConfidence.High),
                    Relationship(E, F, RelationshipConfidence.High),
                    Relationship(A, D, RelationshipConfidence.High),
                ])
            : null);

    private static MatchPremadeParticipant Participant(Guid id, string name, int teamId) =>
        new(id, $"{name}-puuid", name.ToUpperInvariant(), "LAN", teamId);

    private static MatchPremadeRelationship Relationship(
        Guid first,
        Guid second,
        RelationshipConfidence confidence) =>
        new(
            first,
            second,
            confidence == RelationshipConfidence.High ? 5 : 3,
            confidence == RelationshipConfidence.High ? 4 : 2,
            confidence);
}
