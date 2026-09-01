using LolAnalyzer.Application.Riot;

namespace LolAnalyzer.Infrastructure.Riot;

public sealed class RiotRateLimiter : IRiotRateLimiter, IDisposable
{
    private readonly SemaphoreSlim concurrencyGate;
    private readonly TimeProvider timeProvider;
    private readonly object cooldownGate = new();
    private DateTimeOffset retryNotBefore = DateTimeOffset.MinValue;
    private bool disposed;

    public RiotRateLimiter(RiotOptions options, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);

        if (options.RequestConcurrency is < 1 or > 5)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Riot request concurrency must be between 1 and 5.");
        }

        concurrencyGate = new SemaphoreSlim(options.RequestConcurrency, options.RequestConcurrency);
        this.timeProvider = timeProvider;
    }

    public async ValueTask<IDisposable> AcquireAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        while (true)
        {
            await concurrencyGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            var delay = GetRemainingCooldown();
            if (delay <= TimeSpan.Zero)
            {
                return new Lease(concurrencyGate);
            }

            concurrencyGate.Release();
            await Task.Delay(delay, timeProvider, cancellationToken).ConfigureAwait(false);
        }
    }

    public void RegisterRetryAfter(TimeSpan retryAfter)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (retryAfter <= TimeSpan.Zero)
        {
            return;
        }

        var candidate = timeProvider.GetUtcNow().Add(retryAfter);
        lock (cooldownGate)
        {
            if (candidate > retryNotBefore)
            {
                retryNotBefore = candidate;
            }
        }
    }

    public void Dispose()
    {
        disposed = true;
        concurrencyGate.Dispose();
    }

    private TimeSpan GetRemainingCooldown()
    {
        lock (cooldownGate)
        {
            return retryNotBefore - timeProvider.GetUtcNow();
        }
    }

    private sealed class Lease(SemaphoreSlim gate) : IDisposable
    {
        private SemaphoreSlim? gate = gate;

        public void Dispose() => Interlocked.Exchange(ref gate, null)?.Release();
    }
}
