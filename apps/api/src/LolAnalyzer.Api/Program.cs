using System.Net;
using LolAnalyzer.Application.Analysis;
using LolAnalyzer.Application.Matches;
using LolAnalyzer.Application.Players;
using LolAnalyzer.Application.Riot;
using LolAnalyzer.Infrastructure.Health;
using LolAnalyzer.Infrastructure.Persistence;
using LolAnalyzer.Infrastructure.Riot;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var riotOptions = builder.Configuration.GetSection(RiotOptions.SectionName).Get<RiotOptions>() ?? new RiotOptions();
riotOptions.ApiKey = builder.Configuration["RIOT_API_KEY"] ?? riotOptions.ApiKey;
riotOptions.PlatformRegion = builder.Configuration["RIOT_PLATFORM_REGION"] ?? riotOptions.PlatformRegion;
riotOptions.RegionalRouting = builder.Configuration["RIOT_REGIONAL_ROUTING"] ?? riotOptions.RegionalRouting;
if (int.TryParse(builder.Configuration["RIOT_REQUEST_TIMEOUT_SECONDS"], out var configuredTimeout))
{
    riotOptions.RequestTimeoutSeconds = configuredTimeout;
}

if (int.TryParse(builder.Configuration["RIOT_REQUEST_CONCURRENCY"], out var configuredConcurrency))
{
    riotOptions.RequestConcurrency = configuredConcurrency;
}

if (riotOptions.RequestTimeoutSeconds <= 0 || riotOptions.RequestConcurrency is < 1 or > 5)
{
    throw new InvalidOperationException("Riot timeout must be positive and concurrency must be between 1 and 5.");
}

var relationshipScoreOptions = builder.Configuration
    .GetSection(RelationshipScoreOptions.SectionName)
    .Get<RelationshipScoreOptions>() ?? new RelationshipScoreOptions();
relationshipScoreOptions.Validate();
var relationshipAnalysisOptions = builder.Configuration
    .GetSection(PlayerRelationshipAnalysisOptions.SectionName)
    .Get<PlayerRelationshipAnalysisOptions>() ?? new PlayerRelationshipAnalysisOptions();
relationshipAnalysisOptions.Validate();
var premadeDetectionOptions = builder.Configuration
    .GetSection(PremadeDetectionOptions.SectionName)
    .Get<PremadeDetectionOptions>() ?? new PremadeDetectionOptions();
premadeDetectionOptions.Validate();
var premadeGroupDetectionOptions = builder.Configuration
    .GetSection(PremadeGroupDetectionOptions.SectionName)
    .Get<PremadeGroupDetectionOptions>() ?? new PremadeGroupDetectionOptions();
premadeGroupDetectionOptions.Validate();
var playerNetworkOptions = builder.Configuration
    .GetSection(PlayerNetworkOptions.SectionName)
    .Get<PlayerNetworkOptions>() ?? new PlayerNetworkOptions();
playerNetworkOptions.Validate();

var postgresConnectionString = BuildPostgresConnectionString(builder.Configuration)
    ?? throw new InvalidOperationException("ConnectionStrings:Postgres or POSTGRES_HOST must be configured.");
var redisHost = builder.Configuration["REDIS_HOST"] ?? builder.Configuration["Redis:Host"] ?? "localhost";
var redisPort = int.TryParse(builder.Configuration["REDIS_PORT"], out var configuredRedisPort)
    ? configuredRedisPort
    : builder.Configuration.GetValue("Redis:Port", 6379);

builder.Services.AddOpenApi();
builder.Services.AddSingleton(riotOptions);
builder.Services.AddSingleton(new MatchIngestionOptions { RequestConcurrency = riotOptions.RequestConcurrency });
builder.Services.AddSingleton(relationshipScoreOptions);
builder.Services.AddSingleton<RelationshipScoreCalculator>();
builder.Services.AddSingleton(relationshipAnalysisOptions);
builder.Services.AddSingleton<PlayerRelationshipAnalyzer>();
builder.Services.AddSingleton(premadeDetectionOptions);
builder.Services.AddSingleton<PossiblePremadeDetector>();
builder.Services.AddSingleton(premadeGroupDetectionOptions);
builder.Services.AddSingleton<PossiblePremadeGroupDetector>();
builder.Services.AddSingleton(playerNetworkOptions);
builder.Services.AddDbContext<LolAnalyzerDbContext>(options => options.UseNpgsql(postgresConnectionString));
builder.Services.AddScoped<IPlayerRepository, PlayerRepository>();
builder.Services.AddScoped<PlayerLookupService>();
builder.Services.AddScoped<IMatchRepository, MatchRepository>();
builder.Services.AddScoped<MatchIngestionService>();
builder.Services.AddScoped<IPlayerAnalysisRepository, PlayerAnalysisRepository>();
builder.Services.AddScoped<RepeatedPlayerAnalysisService>();
builder.Services.AddScoped<MatchFamiliarityService>();
builder.Services.AddScoped<MatchPremadeGroupService>();
builder.Services.AddScoped<MatchDetailQueryService>();
builder.Services.AddScoped<IPlayerRelationshipRepository, PlayerRelationshipRepository>();
builder.Services.AddScoped<PlayerRelationshipAnalysisService>();
builder.Services.AddScoped<PlayerRelationshipQueryService>();
builder.Services.AddScoped<PlayerNetworkQueryService>();
builder.Services.AddHttpClient<IRiotApiClient, RiotApiClient>(client =>
    {
        client.BaseAddress = riotOptions.GetRegionalBaseUri();
        client.Timeout = Timeout.InfiniteTimeSpan;
    })
    .AddStandardResilienceHandler(options =>
    {
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(riotOptions.RequestTimeoutSeconds);
        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(riotOptions.RequestTimeoutSeconds);
    });
builder.Services.AddHealthChecks()
    .AddCheck<PostgresHealthCheck>("postgresql", tags: ["ready"])
    .AddCheck("redis", new RedisHealthCheck(redisHost, redisPort), tags: ["ready"]);

var app = builder.Build();

if (builder.Configuration.GetValue("Database:ApplyMigrations", true))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<LolAnalyzerDbContext>();
    await dbContext.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = registration => registration.Tags.Contains("ready") });

app.MapGet("/api/v1/players/by-riot-id/{gameName}/{tagLine}", async Task<IResult> (
    string gameName,
    string tagLine,
    PlayerLookupService lookupService,
    RiotOptions options,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(gameName) || string.IsNullOrWhiteSpace(tagLine))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["riotId"] = ["gameName and tagLine are required."],
        });
    }

    try
    {
        var player = await lookupService.FindByRiotIdAsync(
            gameName,
            tagLine,
            options.PlatformRegion,
            cancellationToken);

        return player is null
            ? Results.NotFound()
            : Results.Ok(new PlayerResponse(player.Puuid, player.GameName, player.TagLine, player.PlatformRegion));
    }
    catch (RiotApiException exception) when (exception.StatusCode == HttpStatusCode.TooManyRequests)
    {
        return Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Riot API rate limit reached.");
    }
    catch (RiotApiException exception) when (exception.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
    {
        return Results.Problem(statusCode: StatusCodes.Status502BadGateway, title: "Riot API authorization failed.");
    }
    catch (RiotApiException)
    {
        return Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Riot API is unavailable.");
    }
    catch (InvalidOperationException)
    {
        return Results.Problem(
            statusCode: StatusCodes.Status503ServiceUnavailable,
            title: "Riot API is not configured.");
    }
});

app.MapPost("/api/v1/players/{puuid}/matches/sync", async Task<IResult> (
    string puuid,
    int? count,
    MatchIngestionService ingestionService,
    RepeatedPlayerAnalysisService analysisService,
    PlayerRelationshipAnalysisService relationshipAnalysisService,
    RiotOptions options,
    CancellationToken cancellationToken) =>
{
    const int defaultCount = 20;
    const int maximumCount = 20;
    var requestedCount = count ?? defaultCount;
    if (string.IsNullOrWhiteSpace(puuid) || requestedCount is < 1 or > maximumCount)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["sync"] = [$"puuid is required and count must be between 1 and {maximumCount}."],
        });
    }

    try
    {
        var result = await ingestionService
            .SynchronizeAsync(puuid, requestedCount, options.PlatformRegion, cancellationToken)
            .ConfigureAwait(false);
        var analysis = await analysisService.RebuildAsync(puuid, cancellationToken).ConfigureAwait(false);
        var relationshipAnalysis = await relationshipAnalysisService
            .RebuildAsync(cancellationToken)
            .ConfigureAwait(false);
        return Results.Ok(new
        {
            result.RequestedCount,
            result.MatchIdsReturned,
            result.AlreadyStored,
            result.Downloaded,
            result.Persisted,
            result.NotFound,
            Analysis = analysis,
            RelationshipAnalysis = relationshipAnalysis,
        });
    }
    catch (RiotApiException exception) when (exception.StatusCode == HttpStatusCode.TooManyRequests)
    {
        return Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Riot API rate limit reached.");
    }
    catch (RiotApiException exception) when (exception.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
    {
        return Results.Problem(statusCode: StatusCodes.Status502BadGateway, title: "Riot API authorization failed.");
    }
    catch (RiotApiException)
    {
        return Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Riot API is unavailable.");
    }
    catch (InvalidOperationException)
    {
        return Results.Problem(
            statusCode: StatusCodes.Status503ServiceUnavailable,
            title: "Riot API is not configured.");
    }
});

app.MapGet("/api/v1/players/{puuid}/summary", async Task<IResult> (
    string puuid,
    IPlayerAnalysisRepository repository,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(puuid))
    {
        return Results.BadRequest();
    }

    var summary = await repository.GetSummaryAsync(puuid, cancellationToken).ConfigureAwait(false);
    return summary is null ? Results.NotFound() : Results.Ok(summary);
});

app.MapGet("/api/v1/players/{puuid}/encounters", async Task<IResult> (
    string puuid,
    IPlayerAnalysisRepository repository,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(puuid))
    {
        return Results.BadRequest();
    }

    var encounters = await repository.GetRepeatedPlayersAsync(puuid, cancellationToken).ConfigureAwait(false);
    return encounters is null ? Results.NotFound() : Results.Ok(encounters);
});

app.MapGet("/api/v1/players/{puuid}/matches", async Task<IResult> (
    string puuid,
    int? page,
    int? pageSize,
    int? queue,
    IPlayerAnalysisRepository repository,
    CancellationToken cancellationToken) =>
{
    const int maximumPage = 10_000;
    var requestedPage = page ?? 1;
    var requestedPageSize = pageSize ?? 20;
    if (string.IsNullOrWhiteSpace(puuid)
        || requestedPage is < 1 or > maximumPage
        || requestedPageSize is < 1 or > 100)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["pagination"] = [$"puuid is required, page must be between 1 and {maximumPage} and pageSize between 1 and 100."],
        });
    }

    var matches = await repository
        .GetMatchesAsync(puuid, requestedPage, requestedPageSize, queue, cancellationToken)
        .ConfigureAwait(false);
    return matches is null ? Results.NotFound() : Results.Ok(matches);
});

app.MapGet("/api/v1/matches/{matchId}", async Task<IResult> (
    string matchId,
    string? ownerPuuid,
    MatchDetailQueryService queryService,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(matchId))
    {
        return Results.BadRequest();
    }

    var match = await queryService.GetAsync(matchId, ownerPuuid, cancellationToken).ConfigureAwait(false);
    return match is null ? Results.NotFound() : Results.Ok(match);
});

app.MapGet("/api/v1/players/{puuid}/relationships", async Task<IResult> (
    string puuid,
    int? page,
    int? pageSize,
    string? minimumConfidence,
    int? minimumScore,
    PlayerRelationshipQueryService queryService,
    CancellationToken cancellationToken) =>
{
    const int maximumPage = 10_000;
    var requestedPage = page ?? 1;
    var requestedPageSize = pageSize ?? 20;
    var requestedConfidence = RelationshipConfidence.Low;
    var requestedMinimumScore = minimumScore ?? 0;
    if (!string.IsNullOrWhiteSpace(minimumConfidence)
        && !RelationshipConfidenceExtensions.TryParseLabel(minimumConfidence, out requestedConfidence))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["minimumConfidence"] = ["Use LOW, MEDIUM, HIGH or VERY_HIGH."],
        });
    }

    if (string.IsNullOrWhiteSpace(puuid)
        || requestedPage is < 1 or > maximumPage
        || requestedPageSize is < 1 or > 100
        || requestedMinimumScore is < 0 or > 100)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["query"] = [$"puuid is required, page must be between 1 and {maximumPage}, pageSize between 1 and 100 and minimumScore between 0 and 100."],
        });
    }

    var relationships = await queryService
        .GetAsync(puuid, requestedPage, requestedPageSize, requestedConfidence, requestedMinimumScore, cancellationToken)
        .ConfigureAwait(false);
    return relationships is null ? Results.NotFound() : Results.Ok(relationships);
});

app.MapGet("/api/v1/players/{puuid}/network", async Task<IResult> (
    string puuid,
    int? maxNodes,
    int? maxEdges,
    string? minimumConfidence,
    int? minimumScore,
    PlayerNetworkOptions options,
    PlayerNetworkQueryService queryService,
    CancellationToken cancellationToken) =>
{
    var requestedMaxNodes = maxNodes ?? options.MaximumNodes;
    var requestedMaxEdges = maxEdges ?? options.MaximumEdges;
    var requestedMinimumScore = minimumScore ?? 0;
    var requestedConfidence = RelationshipConfidence.Low;
    if (!string.IsNullOrWhiteSpace(minimumConfidence)
        && !RelationshipConfidenceExtensions.TryParseLabel(minimumConfidence, out requestedConfidence))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["minimumConfidence"] = ["Use LOW, MEDIUM, HIGH or VERY_HIGH."],
        });
    }

    if (string.IsNullOrWhiteSpace(puuid)
        || !queryService.LimitsAreValid(requestedMaxNodes, requestedMaxEdges)
        || requestedMinimumScore is < 0 or > 100)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["query"] = [$"puuid is required, maxNodes must be between 1 and {options.MaximumNodes}, maxEdges between 1 and {options.MaximumEdges} and minimumScore between 0 and 100."],
        });
    }

    var network = await queryService
        .GetAsync(
            puuid,
            requestedMaxNodes,
            requestedMaxEdges,
            requestedConfidence,
            requestedMinimumScore,
            cancellationToken)
        .ConfigureAwait(false);
    return network is null ? Results.NotFound() : Results.Ok(network);
});

app.Run();

internal sealed record PlayerResponse(string Puuid, string GameName, string TagLine, string PlatformRegion);

public partial class Program
{
    private static string? BuildPostgresConnectionString(ConfigurationManager configuration)
    {
        var host = configuration["POSTGRES_HOST"];
        if (string.IsNullOrWhiteSpace(host))
        {
            return configuration.GetConnectionString("Postgres");
        }

        var port = int.TryParse(configuration["POSTGRES_PORT"], out var configuredPort) ? configuredPort : 5432;
        var database = configuration["POSTGRES_DB"] ?? "lol_analyzer";
        var username = configuration["POSTGRES_USER"] ?? "lol";
        var password = configuration["POSTGRES_PASSWORD"];
        var connection = $"Host={host};Port={port};Database={database};Username={username}";

        return string.IsNullOrWhiteSpace(password) ? connection : $"{connection};Password={password}";
    }
}
