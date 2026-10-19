using LolAnalyzer.Application.Riot;
using System.Globalization;

namespace LolAnalyzer.Infrastructure.Riot;

public sealed class RiotRateLimiter : IRiotRateLimiter, IDisposable
{
    private readonly SemaphoreSlim concurrencyGate;
    private readonly TimeProvider timeProvider;
    private readonly object cooldownGate = new();
    private readonly Dictionary<string, List<Window>> budgets = new(StringComparer.Ordinal);
    private readonly RiotOptions options;
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
        options.Validate();

        concurrencyGate = new SemaphoreSlim(options.RequestConcurrency, options.RequestConcurrency);
        this.timeProvider = timeProvider;
        this.options = options;
    }

    public ValueTask<IDisposable> AcquireAsync(CancellationToken cancellationToken) => AcquireAsync("default", "default", cancellationToken);

    public async ValueTask<IDisposable> AcquireAsync(string host, string method, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        while (true)
        {
            await concurrencyGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            TimeSpan delay;
            try { delay = GetRemainingCooldown(host, method); }
            catch
            {
                concurrencyGate.Release();
                throw;
            }
            if (delay <= TimeSpan.Zero)
            {
                return new Lease(concurrencyGate);
            }

            concurrencyGate.Release();
            // Timer implementations have finite duration ranges; chunk waiting, never the cooldown itself.
            await Task.Delay(delay > TimeSpan.FromDays(1) ? TimeSpan.FromDays(1) : delay, timeProvider, cancellationToken).ConfigureAwait(false);
        }
    }

    public void RegisterRetryAfter(TimeSpan retryAfter)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (retryAfter <= TimeSpan.Zero)
        {
            return;
        }

        var now = timeProvider.GetUtcNow();
        var candidate = retryAfter >= DateTimeOffset.MaxValue - now ? DateTimeOffset.MaxValue : now.Add(retryAfter);
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

    public void ObserveLimits(string host, string method, string? appLimits, string? appCounts, string? methodLimits, string? methodCounts)
    {
        lock (cooldownGate)
        {
            Observe($"app:{host}", appLimits, appCounts);
            Observe($"method:{host}:{method}", methodLimits, methodCounts);
        }
    }

    private TimeSpan GetRemainingCooldown(string host, string method)
    {
        lock (cooldownGate)
        {
            var now = timeProvider.GetUtcNow();
            var app = GetBudget($"app:{host}", true);
            var perMethod = GetBudget($"method:{host}:{method}", false);
            var delay = retryNotBefore - now;
            foreach (var window in app.Concat(perMethod))
            {
                while (window.Requests.TryPeek(out var oldest) && oldest + window.Duration <= now) window.Requests.Dequeue();
                var until = DateTimeOffset.MinValue;
                if (window.ObservedUntil <= now) window.ObservedCount = 0;
                if (window.ObservedCount >= window.Limit && window.ObservedUntil > until) until = window.ObservedUntil;
                if (window.Requests.Count >= window.Limit && window.Requests.TryPeek(out var first)) until = until > first + window.Duration ? until : first + window.Duration;
                if (until - now > delay) delay = until - now;
            }
            if (delay <= TimeSpan.Zero)
            {
                foreach (var window in app.Concat(perMethod))
                {
                    window.Requests.Enqueue(now);
                    if (window.ObservedUntil > now) window.ObservedCount++;
                }
            }
            return delay;
        }
    }

    private List<Window> GetBudget(string key, bool application)
    {
        if (budgets.TryGetValue(key, out var existing)) return existing;
        // Only controlled routing/method labels enter production. Fail closed on unexpected cardinality.
        if (budgets.Count >= 256) throw new InvalidOperationException("Riot quota scope capacity exceeded.");
        List<Window> created = application
            ? [new(options.ShortWindowRequests, TimeSpan.FromSeconds(options.ShortWindowSeconds)), new(options.LongWindowRequests, TimeSpan.FromSeconds(options.LongWindowSeconds))]
            : [];
        budgets.Add(key, created);
        return created;
    }

    private void Observe(string key, string? limits, string? counts)
    {
        if (string.IsNullOrWhiteSpace(limits) || limits.Length > 256) return;
        var windows = GetBudget(key, key.StartsWith("app:", StringComparison.Ordinal));
        var observedCounts = ParsePairs(counts).ToDictionary(pair => pair.Seconds, pair => pair.Count);
        foreach (var pair in ParsePairs(limits))
        {
            var duration = TimeSpan.FromSeconds(pair.Seconds);
            var window = windows.FirstOrDefault(value => value.Duration == duration);
            if (window is null)
            {
                if (windows.Count >= 6) continue;
                window = new Window(pair.Count, duration);
                windows.Add(window);
            }
            else window.Limit = Math.Min(window.Limit, pair.Count);
            if (observedCounts.TryGetValue(pair.Seconds, out var count))
            {
                var now = timeProvider.GetUtcNow();
                if (window.ObservedUntil <= now) window.ObservedCount = 0;
                if (count > window.ObservedCount)
                {
                    window.ObservedCount = count;
                    // Header counters do not expose the reset instant. Waiting a full window is conservative.
                    window.ObservedUntil = now + duration;
                }
            }
        }
    }

    private static IEnumerable<(int Count, int Seconds)> ParsePairs(string? header)
    {
        if (header is null || header.Length > 256) yield break;
        var seen = new HashSet<int>();
        foreach (var item in header.Split(',').Take(6))
        {
            var values = item.Split(':');
            if (values.Length == 2 && int.TryParse(values[0], NumberStyles.None, CultureInfo.InvariantCulture, out var count)
                && int.TryParse(values[1], NumberStyles.None, CultureInfo.InvariantCulture, out var seconds)
                && count is > 0 and <= 10000 && seconds is > 0 and <= 3600 && seen.Add(seconds)) yield return (count, seconds);
        }
    }

    private sealed class Window(int limit, TimeSpan duration)
    {
        public int Limit { get; set; } = limit;
        public TimeSpan Duration { get; } = duration;
        public DateTimeOffset ObservedUntil { get; set; }
        public int ObservedCount { get; set; }
        public Queue<DateTimeOffset> Requests { get; } = new();
    }

    private sealed class Lease(SemaphoreSlim gate) : IDisposable
    {
        private SemaphoreSlim? gate = gate;

        public void Dispose() => Interlocked.Exchange(ref gate, null)?.Release();
    }
}
