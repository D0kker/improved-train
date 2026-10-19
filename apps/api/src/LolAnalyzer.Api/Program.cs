using System.Net;
using LolAnalyzer.Api;
using LolAnalyzer.Application.Analysis;
using LolAnalyzer.Application.Caching;
using LolAnalyzer.Application.Observability;
using LolAnalyzer.Application.Riot;
using LolAnalyzer.Application.Stateless;
using LolAnalyzer.Infrastructure.Caching;
using LolAnalyzer.Infrastructure.Riot;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 64 * 1024;
    options.Limits.MaxRequestLineSize = 2048;
    options.Limits.MaxRequestHeadersTotalSize = 16 * 1024;
});
var resourceOptions = builder.Configuration.GetSection(ResourceProtectionOptions.SectionName).Get<ResourceProtectionOptions>() ?? new ResourceProtectionOptions();
resourceOptions.ConcurrencyLimit = builder.Configuration.GetValue("API_CONCURRENCY_LIMIT", resourceOptions.ConcurrencyLimit);
resourceOptions.QueueLimit = builder.Configuration.GetValue("API_QUEUE_LIMIT", resourceOptions.QueueLimit);
resourceOptions.Validate();
var riotOptions = builder.Configuration.GetSection(RiotOptions.SectionName).Get<RiotOptions>() ?? new RiotOptions();
riotOptions.ApiKey = builder.Configuration["RIOT_API_KEY"] ?? riotOptions.ApiKey;
riotOptions.PlatformRegion = builder.Configuration["RIOT_PLATFORM_REGION"] ?? riotOptions.PlatformRegion;
riotOptions.RegionalRouting = builder.Configuration["RIOT_REGIONAL_ROUTING"] ?? riotOptions.RegionalRouting;
riotOptions.RequestTimeoutSeconds = builder.Configuration.GetValue("RIOT_REQUEST_TIMEOUT_SECONDS", riotOptions.RequestTimeoutSeconds);
riotOptions.RequestConcurrency = builder.Configuration.GetValue("RIOT_REQUEST_CONCURRENCY", riotOptions.RequestConcurrency);
riotOptions.MaxRetryAttempts = builder.Configuration.GetValue("RIOT_MAX_RETRY_ATTEMPTS", riotOptions.MaxRetryAttempts);
riotOptions.BaseRetryDelayMilliseconds = builder.Configuration.GetValue("RIOT_BASE_RETRY_DELAY_MILLISECONDS", riotOptions.BaseRetryDelayMilliseconds);
riotOptions.MaxRetryDelaySeconds = builder.Configuration.GetValue("RIOT_MAX_RETRY_DELAY_SECONDS", riotOptions.MaxRetryDelaySeconds);
riotOptions.Validate();
var analysisOptions = builder.Configuration.GetSection(OnDemandAnalysisOptions.SectionName).Get<OnDemandAnalysisOptions>() ?? new OnDemandAnalysisOptions();
analysisOptions.Validate();
var cacheOptions = builder.Configuration.GetSection(CacheOptions.SectionName).Get<CacheOptions>() ?? new CacheOptions();
cacheOptions.Validate();
var scoreOptions = builder.Configuration.GetSection(RelationshipScoreOptions.SectionName).Get<RelationshipScoreOptions>() ?? new RelationshipScoreOptions();
scoreOptions.Validate();
var premadeOptions = builder.Configuration.GetSection(PremadeDetectionOptions.SectionName).Get<PremadeDetectionOptions>() ?? new PremadeDetectionOptions();
premadeOptions.Validate();
var groupOptions = builder.Configuration.GetSection(PremadeGroupDetectionOptions.SectionName).Get<PremadeGroupDetectionOptions>() ?? new PremadeGroupDetectionOptions();
groupOptions.Validate();

builder.Services.AddOpenApi();
builder.Services.AddSingleton(riotOptions);
builder.Services.AddSingleton(analysisOptions);
builder.Services.AddSingleton(resourceOptions);
builder.Services.AddSingleton<ApiResourceGate>();
builder.Services.AddSingleton(cacheOptions);
builder.Services.AddSingleton(scoreOptions);
builder.Services.AddSingleton(premadeOptions);
builder.Services.AddSingleton(groupOptions);
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
builder.Services.AddSingleton<OperationalMetrics>();
builder.Services.AddSingleton<IRiotRateLimiter, RiotRateLimiter>();
builder.Services.AddSingleton<ICacheService, MemoryCacheService>();
builder.Services.AddSingleton<IRiotResponseCache, RiotResponseCache>();
builder.Services.AddSingleton<RelationshipScoreCalculator>();
builder.Services.AddSingleton<PlayerRelationshipAnalyzer>();
builder.Services.AddSingleton<PossiblePremadeDetector>();
builder.Services.AddSingleton<PossiblePremadeGroupDetector>();
builder.Services.AddSingleton<RequestAnalysisCalculator>();
builder.Services.AddScoped<OnDemandAnalysisService>();
builder.Services.AddHttpClient<IRiotApiClient, RiotApiClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(riotOptions.RequestTimeoutSeconds);
    client.MaxResponseContentBufferSize = 1024 * 1024;
});
builder.Services.AddHealthChecks().AddCheck("stateless-runtime", () =>
    Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: ["ready"]);

var app = builder.Build();
app.UseMiddleware<SafeRequestLoggingMiddleware>();
app.Use(async (context, next) =>
{
    context.Response.Headers.XContentTypeOptions = "nosniff";
    context.Response.Headers.XFrameOptions = "DENY";
    context.Response.Headers["Referrer-Policy"] = "same-origin";
    context.Response.Headers["Permissions-Policy"] = "camera=(), geolocation=(), microphone=()";
    if (context.Request.ContentLength > 64 * 1024)
    {
        context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
        return;
    }
    using var budget = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);
    budget.CancelAfter(TimeSpan.FromSeconds(context.RequestServices.GetRequiredService<OnDemandAnalysisOptions>().RequestTimeoutSeconds));
    var disconnected = context.RequestAborted;
    context.RequestAborted = budget.Token;
    try { await next(context).ConfigureAwait(false); }
    catch (OperationCanceledException) when (disconnected.IsCancellationRequested)
    { context.RequestServices.GetRequiredService<OperationalMetrics>().RecordCancellation(false); context.Abort(); }
    catch (Exception) when (context.Response.HasStarted)
    {
        context.RequestServices.GetRequiredService<OperationalMetrics>().RecordCancellation(budget.IsCancellationRequested);
        context.Abort();
    }
    catch (OperationCanceledException) when (!budget.IsCancellationRequested)
    {
        context.RequestServices.GetRequiredService<OperationalMetrics>().RecordCancellation(false);
        await Results.Problem(statusCode: 503, title: "Riot API request timed out.").ExecuteAsync(context);
    }
    catch (OperationCanceledException)
    {
        context.RequestServices.GetRequiredService<OperationalMetrics>().RecordCancellation(true);
        context.RequestAborted = disconnected;
        await Results.Problem(statusCode: 504, title: "Analysis time budget exceeded. Try again; cached Riot data may reduce the duration.").ExecuteAsync(context);
    }
    catch (RiotApiException error)
    {
        var status = error.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden ? 502 : 503;
        await Results.Problem(statusCode: status, title: error.StatusCode == HttpStatusCode.TooManyRequests
            ? "Riot API rate limit reached." : "Riot API is unavailable.").ExecuteAsync(context);
    }
    catch (HttpRequestException)
    { await Results.Problem(statusCode: 503, title: "Riot API is unavailable.").ExecuteAsync(context); }
    catch (BadHttpRequestException error)
    { await Results.Problem(statusCode: error.StatusCode, title: "Invalid request body.").ExecuteAsync(context); }
    catch (ArgumentException)
    { await Results.Problem(statusCode: 400, title: "Invalid player, match, platform or pagination.").ExecuteAsync(context); }
    catch (InvalidOperationException error) when (error.Message.Contains("RIOT_API_KEY", StringComparison.Ordinal))
    { await Results.Problem(statusCode: 503, title: "Riot API is not configured.").ExecuteAsync(context); }
    catch (Exception)
    { await Results.Problem(statusCode: 500, title: "Analysis could not be completed.").ExecuteAsync(context); }
});
app.Use(async (context, next) =>
{
    await context.RequestServices.GetRequiredService<ApiResourceGate>().InvokeAsync(context, async admitted =>
    {
        if (!RequestInputGuard.IsValid(admitted))
        {
            await Results.Problem(statusCode: 400, title: "Invalid or oversized request parameters.").ExecuteAsync(admitted).ConfigureAwait(false);
            return;
        }
        await next(admitted).ConfigureAwait(false);
    }).ConfigureAwait(false);
});
if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = registration => registration.Tags.Contains("ready") });
app.MapGet("/metrics", (OperationalMetrics metrics, ICacheService cache) =>
{
    if (cache is MemoryCacheService memory) metrics.RecordCacheSize(memory.Count, memory.EstimatedBytes);
    return Results.Ok(metrics.Snapshot());
});

app.MapGet("/api/v1/players/by-riot-id/{gameName}/{tagLine}/analysis", async Task<IResult> (
    string gameName, string tagLine, string? platformRegion, OnDemandAnalysisService service, RiotOptions configuration, ILogger<Program> logger, CancellationToken token) =>
{
    var result = await service.SearchAsync(gameName, tagLine, platformRegion ?? configuration.PlatformRegion, token).ConfigureAwait(false);
    if (result is not null) AnalysisLog.Completed(logger, "player-search", 1, result.Coverage.MatchIdsObtained,
        result.Coverage.UniqueMatchIds, result.Coverage.MatchesRecovered, result.Coverage.Failures.Count, result.Coverage.Status);
    return result is null ? Results.NotFound() : Results.Ok(result);
});
app.MapGet("/api/v1/players/by-riot-id/{gameName}/{tagLine}", async Task<IResult> (
    string gameName, string tagLine, string? platformRegion, IRiotApiClient riot, RiotOptions configuration, CancellationToken token) =>
{
    var platform = platformRegion ?? configuration.PlatformRegion;
    var account = await riot.ForPlatform(platform).GetAccountByRiotIdAsync(gameName, tagLine, token).ConfigureAwait(false);
    return account is null ? Results.NotFound() : Results.Ok(new PlayerIdentity(account.Puuid, account.GameName, account.TagLine, platform));
});
app.MapGet("/api/v1/players/{puuid}/recent-matches", async Task<IResult> (
    string puuid, int? start, int? count, string? platformRegion, OnDemandAnalysisService service, RiotOptions configuration, CancellationToken token) =>
{
    if (start is < 0 or > 1000) return Results.BadRequest();
    return Results.Ok(await service.RecentAsync(puuid, platformRegion ?? configuration.PlatformRegion, start ?? 0, count ?? 5, token).ConfigureAwait(false));
});
app.MapPost("/api/v1/matches/{matchId}/analyze", async Task<IResult> (
    string matchId, AnalyzeMatchRequest request, OnDemandAnalysisService service, RiotOptions configuration, ILogger<Program> logger, CancellationToken token) =>
{
    var result = await service.AnalyzeAsync(matchId, request.OwnerPuuid, RiotRouting.PlatformForMatch(matchId, request.PlatformRegion, configuration.PlatformRegion), token).ConfigureAwait(false);
    if (result is not null) AnalysisLog.Completed(logger, "selected-match", result.Coverage.Participants,
        result.Coverage.MatchIdsObtained, result.Coverage.UniqueMatchIds, result.Coverage.MatchesRecovered,
        result.Coverage.Failures.Count, result.Coverage.Status);
    return result is null ? Results.NotFound() : Results.Ok(result);
});
app.MapGet("/api/v1/matches/{matchId}", async Task<IResult> (
    string matchId, string? ownerPuuid, string? platformRegion, OnDemandAnalysisService service, IRiotApiClient riot, RiotOptions configuration, CancellationToken token) =>
{
    var platform = RiotRouting.PlatformForMatch(matchId, platformRegion, configuration.PlatformRegion);
    if (!string.IsNullOrWhiteSpace(ownerPuuid))
    {
        var analysis = await service.AnalyzeAsync(matchId, ownerPuuid, platform, token).ConfigureAwait(false);
        return analysis is null ? Results.NotFound() : Results.Ok(analysis.Match);
    }
    var match = await riot.ForPlatform(platform).GetMatchAsync(matchId, token).ConfigureAwait(false);
    return match is null ? Results.NotFound() : Results.Ok(RequestAnalysisCalculator.Detail(match));
});
app.MapGet("/api/v1/players/{puuid}/active-game", async Task<IResult> (
    string puuid, string? platformRegion, IRiotApiClient riot, RiotOptions configuration, CancellationToken token) =>
    Results.Ok(await riot.ForPlatform(platformRegion ?? configuration.PlatformRegion).GetActiveGameAsync(puuid, token).ConfigureAwait(false)));

// Compatibility reads fetch their entire window independently from Riot.
app.MapGet("/api/v1/players/{puuid}/summary", async (string puuid, OnDemandAnalysisService service, RiotOptions configuration, CancellationToken token) =>
    (await service.RecentAsync(puuid, configuration.PlatformRegion, 0, 5, token).ConfigureAwait(false)).Summary);
app.MapGet("/api/v1/players/{puuid}/known-players", async (string puuid, OnDemandAnalysisService service, RiotOptions configuration, CancellationToken token) =>
    (await service.RecentAsync(puuid, configuration.PlatformRegion, 0, 5, token).ConfigureAwait(false)).Encounters);
app.MapGet("/api/v1/players/{puuid}/encounters", async (string puuid, OnDemandAnalysisService service, RiotOptions configuration, CancellationToken token) =>
    (await service.RecentAsync(puuid, configuration.PlatformRegion, 0, 5, token).ConfigureAwait(false)).Encounters.Where(player => player.TotalMatches >= 2));
app.MapGet("/api/v1/players/{puuid}/matches", async Task<IResult> (
    string puuid, int? page, int? pageSize, OnDemandAnalysisService service, RiotOptions configuration, CancellationToken token) =>
{
    if ((page ?? 1) is < 1 or > 200 || (pageSize ?? 5) is < 1 or > 5) return Results.BadRequest();
    return Results.Ok((await service.RecentAsync(puuid, configuration.PlatformRegion, ((page ?? 1) - 1) * (pageSize ?? 5), pageSize ?? 5, token).ConfigureAwait(false)).Matches);
});
foreach (var retired in new[] { "/api/v1/players/{puuid}/analysis", "/api/v1/jobs/{jobId}", "/api/v1/jobs/{jobId}/cancel",
    "/api/v1/players/{puuid}/refresh-schedule", "/api/v1/players/{puuid}/matches/sync",
    "/api/v1/players/{puuid}/relationships", "/api/v1/players/{puuid}/network" })
    app.MapMethods(retired, ["GET", "POST", "PUT"], () => Results.Problem(statusCode: 410,
        title: "This stateful endpoint has been retired. Use recent-matches or POST /matches/{matchId}/analyze."));

app.Run();

public partial class Program;
