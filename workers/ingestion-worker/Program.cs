using LolAnalyzer.Application.Analysis;
using LolAnalyzer.Application.Caching;
using LolAnalyzer.Application.Jobs;
using LolAnalyzer.Application.Matches;
using LolAnalyzer.Application.Observability;
using LolAnalyzer.Application.Riot;
using LolAnalyzer.Infrastructure.Health;
using LolAnalyzer.Infrastructure.Caching;
using LolAnalyzer.Infrastructure.Persistence;
using LolAnalyzer.Infrastructure.Riot;
using LolAnalyzer.IngestionWorker;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

var postgresConnectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("ConnectionStrings:Postgres must be configured.");
var redisHost = builder.Configuration["REDIS_HOST"] ?? builder.Configuration["Redis:Host"] ?? "localhost";
var redisPort = int.TryParse(builder.Configuration["REDIS_PORT"], out var configuredRedisPort)
    ? configuredRedisPort
    : builder.Configuration.GetValue("Redis:Port", 6379);

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

if (int.TryParse(builder.Configuration["RIOT_MAX_RETRY_ATTEMPTS"], out var configuredRetryAttempts))
{
    riotOptions.MaxRetryAttempts = configuredRetryAttempts;
}

if (int.TryParse(builder.Configuration["RIOT_BASE_RETRY_DELAY_MILLISECONDS"], out var configuredBaseRetryDelay))
{
    riotOptions.BaseRetryDelayMilliseconds = configuredBaseRetryDelay;
}

if (int.TryParse(builder.Configuration["RIOT_MAX_RETRY_DELAY_SECONDS"], out var configuredMaxRetryDelay))
{
    riotOptions.MaxRetryDelaySeconds = configuredMaxRetryDelay;
}

riotOptions.Validate();

var jobOptions = builder.Configuration
    .GetSection(AnalysisJobExecutionOptions.SectionName)
    .Get<AnalysisJobExecutionOptions>() ?? new AnalysisJobExecutionOptions();
jobOptions.Validate();
var relationshipScoreOptions = builder.Configuration
    .GetSection(RelationshipScoreOptions.SectionName)
    .Get<RelationshipScoreOptions>() ?? new RelationshipScoreOptions();
relationshipScoreOptions.Validate();
var relationshipAnalysisOptions = builder.Configuration
    .GetSection(PlayerRelationshipAnalysisOptions.SectionName)
    .Get<PlayerRelationshipAnalysisOptions>() ?? new PlayerRelationshipAnalysisOptions();
relationshipAnalysisOptions.Validate();
var cacheOptions = builder.Configuration
    .GetSection(CacheOptions.SectionName)
    .Get<CacheOptions>() ?? new CacheOptions();
if (int.TryParse(builder.Configuration["CACHE_PLAYER_SUMMARY_TTL_SECONDS"], out var configuredSummaryTtl))
{
    cacheOptions.PlayerSummaryTtlSeconds = configuredSummaryTtl;
}

cacheOptions.Validate();

builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
builder.Services.AddSingleton<OperationalMetrics>();
builder.Services.AddSingleton<IRiotRateLimiter, RiotRateLimiter>();
builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(new ConfigurationOptions
{
    EndPoints = { { redisHost, redisPort } },
    AbortOnConnectFail = false,
    ConnectRetry = 1,
    ConnectTimeout = 500,
    AsyncTimeout = 500,
    SyncTimeout = 500,
}));
builder.Services.AddSingleton<IRedisCacheStore, StackExchangeRedisCacheStore>();
builder.Services.AddSingleton<MemoryCacheService>();
builder.Services.AddSingleton<ICacheService, RedisCacheService>();
builder.Services.AddSingleton(riotOptions);
builder.Services.AddSingleton(cacheOptions);
builder.Services.AddSingleton(jobOptions);
builder.Services.AddSingleton(new MatchIngestionOptions { RequestConcurrency = riotOptions.RequestConcurrency });
builder.Services.AddSingleton(relationshipScoreOptions);
builder.Services.AddSingleton<RelationshipScoreCalculator>();
builder.Services.AddSingleton(relationshipAnalysisOptions);
builder.Services.AddSingleton<PlayerRelationshipAnalyzer>();
builder.Services.AddDbContext<LolAnalyzerDbContext>(options => options.UseNpgsql(postgresConnectionString));
builder.Services.AddScoped<IAnalysisJobRepository, AnalysisJobRepository>();
builder.Services.AddScoped<IPlayerRefreshScheduleRepository, PlayerRefreshScheduleRepository>();
builder.Services.AddScoped<PlayerRefreshScheduleService>();
builder.Services.AddScoped<IMatchRepository, MatchRepository>();
builder.Services.AddScoped<MatchIngestionService>();
builder.Services.AddScoped<IPlayerAnalysisRepository, PlayerAnalysisRepository>();
builder.Services.AddScoped<RepeatedPlayerAnalysisService>();
builder.Services.AddScoped<IPlayerRelationshipRepository, PlayerRelationshipRepository>();
builder.Services.AddScoped<PlayerRelationshipAnalysisService>();
builder.Services.AddScoped<AnalysisJobExecutionService>();
builder.Services.AddHttpClient<IRiotApiClient, RiotApiClient>(client =>
    {
        client.BaseAddress = riotOptions.GetRegionalBaseUri();
        client.Timeout = TimeSpan.FromSeconds(riotOptions.RequestTimeoutSeconds);
    });
builder.Services.AddHostedService<WorkerProcess>();
builder.Services.AddHealthChecks()
    .AddCheck<PostgresHealthCheck>("postgresql", tags: ["ready"])
    .AddCheck("redis", new RedisHealthCheck(redisHost, redisPort), tags: ["ready"]);

var app = builder.Build();

app.Use(async (context, next) =>
{
    var startedAt = TimeProvider.System.GetTimestamp();
    try
    {
        await next(context).ConfigureAwait(false);
    }
    finally
    {
        var duration = TimeProvider.System.GetElapsedTime(startedAt);
        context.RequestServices.GetRequiredService<OperationalMetrics>()
            .RecordHttpRequest(context.Request.Method, context.Response.StatusCode, duration);
    }
});

app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks(
    "/health/ready",
    new HealthCheckOptions { Predicate = registration => registration.Tags.Contains("ready") });
app.MapGet("/metrics", (OperationalMetrics metrics) => Results.Ok(metrics.Snapshot()));

await app.RunAsync();
