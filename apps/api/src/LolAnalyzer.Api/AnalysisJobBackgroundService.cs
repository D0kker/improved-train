using System.Diagnostics;
using System.Net;
using LolAnalyzer.Application.Jobs;
using LolAnalyzer.Application.Observability;
using LolAnalyzer.Application.Riot;
using LolAnalyzer.Infrastructure.Riot;

namespace LolAnalyzer.Api;

public sealed partial class AnalysisJobBackgroundService(
    IAnalysisJobDispatcher queue,
    IServiceScopeFactory scopeFactory,
    AnalysisJobExecutionOptions options,
    RiotOptions riotOptions,
    TimeProvider timeProvider,
    OperationalMetrics metrics,
    ILogger<AnalysisJobBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogReady(logger, options.QueueCapacity);
        await foreach (var _ in queue.ReadAllAsync(stoppingToken).ConfigureAwait(false))
        {
            await ProcessNextAsync(stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task ProcessNextAsync(CancellationToken stoppingToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IAnalysisJobRepository>();
        var now = timeProvider.GetUtcNow();
        var job = await repository
            .ClaimNextAsync(now, now - options.LeaseTimeout, stoppingToken)
            .ConfigureAwait(false);
        if (job is null)
        {
            return;
        }

        var startedAt = Stopwatch.GetTimestamp();
        var succeeded = false;
        try
        {
            metrics.StartJob();
            LogStarted(logger, job.Id, job.MatchesProcessed, job.RequestedCount);
            var executor = scope.ServiceProvider.GetRequiredService<AnalysisJobExecutionService>();
            await executor.ExecuteAsync(job, riotOptions.PlatformRegion, stoppingToken).ConfigureAwait(false);
            succeeded = true;
            LogStopped(logger, job.Id);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            using var releaseTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await repository.RequeueAsync(job.Id, timeProvider.GetUtcNow(), releaseTimeout.Token).ConfigureAwait(false);
            throw;
        }
        catch (RiotApiException exception)
        {
            var errorCode = exception.StatusCode switch
            {
                HttpStatusCode.TooManyRequests => "riot_rate_limited",
                HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => "riot_authentication_failed",
                _ => "riot_unavailable",
            };
            await repository.FailAsync(job.Id, errorCode, timeProvider.GetUtcNow(), stoppingToken).ConfigureAwait(false);
            LogFailed(logger, job.Id, errorCode);
        }
        catch (InvalidOperationException exception)
        {
            var errorCode = exception.Message.Contains("RIOT_API_KEY", StringComparison.Ordinal)
                ? "riot_not_configured"
                : "configuration_error";
            await repository.FailAsync(job.Id, errorCode, timeProvider.GetUtcNow(), stoppingToken).ConfigureAwait(false);
            LogFailed(logger, job.Id, errorCode);
        }
        catch (Exception exception)
        {
            await repository.FailAsync(job.Id, "internal_error", timeProvider.GetUtcNow(), stoppingToken).ConfigureAwait(false);
            LogUnexpected(logger, job.Id, exception);
        }
        finally
        {
            metrics.FinishJob(Stopwatch.GetElapsedTime(startedAt), succeeded);
        }
    }

    [LoggerMessage(EventId = 20, Level = LogLevel.Information, Message = "In-process analysis job queue is ready with capacity {Capacity}.")]
    private static partial void LogReady(ILogger logger, int capacity);

    [LoggerMessage(EventId = 21, Level = LogLevel.Information, Message = "Analysis job {JobId} started at {Processed}/{Requested}.")]
    private static partial void LogStarted(ILogger logger, Guid jobId, int processed, int requested);

    [LoggerMessage(EventId = 22, Level = LogLevel.Information, Message = "Analysis job {JobId} reached a terminal state.")]
    private static partial void LogStopped(ILogger logger, Guid jobId);

    [LoggerMessage(EventId = 23, Level = LogLevel.Warning, Message = "Analysis job {JobId} failed with safe code {ErrorCode}.")]
    private static partial void LogFailed(ILogger logger, Guid jobId, string errorCode);

    [LoggerMessage(EventId = 24, Level = LogLevel.Error, Message = "Analysis job {JobId} failed unexpectedly.")]
    private static partial void LogUnexpected(ILogger logger, Guid jobId, Exception exception);
}
