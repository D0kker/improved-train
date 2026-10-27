using LolAnalyzer.Application.Observability;

namespace LolAnalyzer.Api;

public sealed partial class SafeRequestLoggingMiddleware(
    RequestDelegate next,
    OperationalMetrics metrics,
    ILogger<SafeRequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var startedAt = TimeProvider.System.GetTimestamp();
        try
        {
            await next(context).ConfigureAwait(false);
        }
        finally
        {
            var duration = TimeProvider.System.GetElapsedTime(startedAt);
            metrics.RecordHttpRequest(context.Request.Method, context.Response.StatusCode, duration,
                (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText ?? "unmatched");
            LogRequestCompleted(
                logger,
                context.TraceIdentifier,
                context.Request.Method,
                (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText ?? "unmatched",
                context.Response.StatusCode,
                duration.TotalMilliseconds);
        }
    }

    [LoggerMessage(
        EventId = 100,
        Level = LogLevel.Information,
        Message = "HTTP request completed. CorrelationId={CorrelationId} Method={Method} Endpoint={Endpoint} StatusCode={StatusCode} DurationMs={DurationMs}")]
    private static partial void LogRequestCompleted(
        ILogger logger,
        string correlationId,
        string method,
        string endpoint,
        int statusCode,
        double durationMs);
}
