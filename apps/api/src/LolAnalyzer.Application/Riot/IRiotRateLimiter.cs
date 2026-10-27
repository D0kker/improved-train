namespace LolAnalyzer.Application.Riot;

public interface IRiotRateLimiter
{
    ValueTask<IDisposable> AcquireAsync(CancellationToken cancellationToken);

    ValueTask<IDisposable> AcquireAsync(string host, string method, CancellationToken cancellationToken) => AcquireAsync(cancellationToken);

    void ObserveLimits(string host, string method, string? appLimits, string? appCounts, string? methodLimits, string? methodCounts) { }

    void RegisterRetryAfter(TimeSpan retryAfter);
}
