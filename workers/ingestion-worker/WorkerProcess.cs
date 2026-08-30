namespace LolAnalyzer.IngestionWorker;

public sealed partial class WorkerProcess(ILogger<WorkerProcess> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogWorkerReady(logger);
        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken).ConfigureAwait(false);
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Ingestion worker is ready; persistent background jobs are planned for Sprint 6.")]
    private static partial void LogWorkerReady(ILogger logger);
}
