using LolAnalyzer.Application.Caching;
using LolAnalyzer.Application.Observability;
using LolAnalyzer.Application.Riot;

namespace LolAnalyzer.Infrastructure.Riot;

public sealed class RiotResponseCache(ICacheService cache, OperationalMetrics? metrics = null) : IRiotResponseCache
{
    private readonly object gate = new();
    private readonly Dictionary<string, Flight> flights = new(StringComparer.Ordinal);

    public async Task<T?> GetOrCreateAsync<T>(string key, TimeSpan timeToLive, string tag,
        Func<CancellationToken, Task<T?>> factory, CancellationToken cancellationToken) where T : class
    {
        cancellationToken.ThrowIfCancellationRequested();
        T? cached = null;
        try { cached = await cache.GetAsync<T>(key, cancellationToken).ConfigureAwait(false); }
        catch (Exception) when (!cancellationToken.IsCancellationRequested) { /* Optional cache: fetch from Riot. */ }
        metrics?.RecordCacheAccess(cached is not null, false);
        if (cached is not null) return cached;
        Flight pending;
        lock (gate)
        {
            if (!flights.TryGetValue(key, out pending!))
            {
                pending = new Flight(async flight =>
                {
                    var value = await factory(flight.Cancellation.Token).ConfigureAwait(false);
                    if (value is not null)
                    {
                        try { await cache.SetAsync(key, value, timeToLive, tag, flight.Cancellation.Token).ConfigureAwait(false); }
                        catch (Exception) when (!flight.Cancellation.IsCancellationRequested) { /* Cache failure cannot invalidate a result. */ }
                    }
                    return value;
                });
                flights.Add(key, pending);
            }
            pending.Users++;
        }
        var work = pending.Work.Value;
        lock (gate)
        {
            if (!pending.Observed)
            {
                pending.Observed = true;
                _ = work.ContinueWith(completed =>
                {
                    // Observe exceptions even when every HTTP caller has disconnected.
                    _ = completed.Exception;
                    lock (gate)
                    {
                        if (flights.GetValueOrDefault(key) == pending) flights.Remove(key);
                        pending.Cancellation.Dispose();
                    }
                }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            }
        }
        try { return (T?)await work.WaitAsync(cancellationToken).ConfigureAwait(false); }
        finally
        {
            lock (gate)
            {
                pending.Users--;
                if (pending.Users == 0 && !work.IsCompleted)
                {
                    if (flights.GetValueOrDefault(key) == pending) flights.Remove(key);
                    pending.Cancellation.Cancel();
                }
            }
        }
    }

    private sealed class Flight
    {
        public CancellationTokenSource Cancellation { get; } = new();
        public Lazy<Task<object?>> Work { get; }
        public int Users { get; set; }
        public bool Observed { get; set; }

        public Flight(Func<Flight, Task<object?>> factory)
        {
            Work = new Lazy<Task<object?>>(() => factory(this), LazyThreadSafetyMode.ExecutionAndPublication);
        }
    }
}
