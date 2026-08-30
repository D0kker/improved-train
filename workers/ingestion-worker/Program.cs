using LolAnalyzer.Infrastructure.Health;
using LolAnalyzer.Infrastructure.Persistence;
using LolAnalyzer.IngestionWorker;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var postgresConnectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("ConnectionStrings:Postgres must be configured.");
var redisHost = builder.Configuration["Redis:Host"] ?? "localhost";
var redisPort = builder.Configuration.GetValue("Redis:Port", 6379);

builder.Services.AddDbContext<LolAnalyzerDbContext>(options => options.UseNpgsql(postgresConnectionString));
builder.Services.AddHostedService<WorkerProcess>();
builder.Services.AddHealthChecks()
    .AddCheck<PostgresHealthCheck>("postgresql", tags: ["ready"])
    .AddCheck("redis", new RedisHealthCheck(redisHost, redisPort), tags: ["ready"]);

var app = builder.Build();

app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks(
    "/health/ready",
    new HealthCheckOptions { Predicate = registration => registration.Tags.Contains("ready") });

await app.RunAsync();
