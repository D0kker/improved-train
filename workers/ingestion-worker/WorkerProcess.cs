using System.Net;
using System.Diagnostics;
using LolAnalyzer.Application.Jobs;
using LolAnalyzer.Application.Observability;
using LolAnalyzer.Infrastructure.Riot;

namespace LolAnalyzer.IngestionWorker;

public sealed partial class WorkerProcess(
    IServiceScopeFactory scopeFactory,
    AnalysisJobExecutionOptions options,
    RiotOptions riotOptions,
    TimeProvider timeProvider,
    OperationalMetrics metrics,
    ILogger<WorkerProcess> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogWorkerReady(logger);
        while (!stoppingToken.IsCancellationRequested)
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var repository = scope.ServiceProvider.GetRequiredService<IAnalysisJobRepository>();
            var scheduleService = scope.ServiceProvider.GetRequiredService<PlayerRefreshScheduleService>();
            await scheduleService.EnqueueNextDueAsync(stoppingToken).ConfigureAwait(false);
            var now = timeProvider.GetUtcNow();
            var job = await repository
                .ClaimNextAsync(now, now - options.LeaseTimeout, stoppingToken)
                .ConfigureAwait(false);
            if (job is null)
            {
                await Task.Delay(options.PollInterval, stoppingToken).ConfigureAwait(false);
                continue;
            }

            try
            {
                metrics.StartJob();
                var startedAt = Stopwatch.GetTimestamp();
                var succeeded = false;
                try
                {
                    LogJobStarted(logger, job.Id, job.MatchesProcessed, job.RequestedCount);
                    var executor = scope.ServiceProvider.GetRequiredService<AnalysisJobExecutionService>();
                    await executor
                        .ExecuteAsync(job, riotOptions.PlatformRegion, stoppingToken)
                        .ConfigureAwait(false);
                    succeeded = true;
                    LogJobStopped(logger, job.Id);
                }
                finally
                {
                    metrics.FinishJob(Stopwatch.GetElapsedTime(startedAt), succeeded);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                using var releaseTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await repository
                    .RequeueAsync(job.Id, timeProvider.GetUtcNow(), releaseTimeout.Token)
                    .ConfigureAwait(false);
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
                await repository
                    .FailAsync(job.Id, errorCode, timeProvider.GetUtcNow(), stoppingToken)
                    .ConfigureAwait(false);
                LogJobFailed(logger, job.Id, errorCode);
            }
            catch (InvalidOperationException exception)
            {
                var errorCode = exception.Message.Contains("RIOT_API_KEY", StringComparison.Ordinal)
                    ? "riot_not_configured"
                    : "configuration_error";
                await repository
                    .FailAsync(job.Id, errorCode, timeProvider.GetUtcNow(), stoppingToken)
                    .ConfigureAwait(false);
                LogJobFailed(logger, job.Id, errorCode);
            }
            catch (Exception exception)
            {
                const string errorCode = "internal_error";
                await repository
                    .FailAsync(job.Id, errorCode, timeProvider.GetUtcNow(), stoppingToken)
                    .ConfigureAwait(false);
                LogUnexpectedFailure(logger, job.Id, exception);
            }
        }
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Ingestion worker is ready for persistent analysis jobs.")]
    private static partial void LogWorkerReady(ILogger logger);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Analysis job {JobId} started at {Processed}/{Requested}.")]
    private static partial void LogJobStarted(ILogger logger, Guid jobId, int processed, int requested);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "Analysis job {JobId} stopped after reaching a terminal state.")]
    private static partial void LogJobStopped(ILogger logger, Guid jobId);

    [LoggerMessage(EventId = 4, Level = LogLevel.Warning, Message = "Analysis job {JobId} failed with safe code {ErrorCode}.")]
    private static partial void LogJobFailed(ILogger logger, Guid jobId, string errorCode);

    [LoggerMessage(EventId = 5, Level = LogLevel.Error, Message = "Analysis job {JobId} failed unexpectedly.")]
    private static partial void LogUnexpectedFailure(ILogger logger, Guid jobId, Exception exception);
}
