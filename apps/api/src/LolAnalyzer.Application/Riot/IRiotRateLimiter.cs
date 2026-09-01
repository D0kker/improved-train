namespace LolAnalyzer.Application.Riot;

public interface IRiotRateLimiter
{
    ValueTask<IDisposable> AcquireAsync(CancellationToken cancellationToken);

    void RegisterRetryAfter(TimeSpan retryAfter);
}
