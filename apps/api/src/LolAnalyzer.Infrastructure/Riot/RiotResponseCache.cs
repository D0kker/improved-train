using System.Collections.Concurrent;
using LolAnalyzer.Application.Caching;
using LolAnalyzer.Application.Riot;

namespace LolAnalyzer.Infrastructure.Riot;

public sealed class RiotResponseCache(ICacheService cache) : IRiotResponseCache
{
    private readonly ConcurrentDictionary<string, Lazy<Task<object?>>> inFlight = new(StringComparer.Ordinal);

    public async Task<T?> GetOrCreateAsync<T>(
        string key,
        TimeSpan timeToLive,
        string tag,
        Func<CancellationToken, Task<T?>> factory,
        CancellationToken cancellationToken)
        where T : class
    {
        var cached = await cache.GetAsync<T>(key, cancellationToken).ConfigureAwait(false);
        if (cached is not null)
        {
            return cached;
        }

        var pending = inFlight.GetOrAdd(
            key,
            _ => new Lazy<Task<object?>>(
                async () =>
                {
                    var value = await factory(cancellationToken).ConfigureAwait(false);
                    if (value is not null)
                    {
                        await cache.SetAsync(key, value, timeToLive, tag, CancellationToken.None).ConfigureAwait(false);
                    }

                    return value;
                },
                LazyThreadSafetyMode.ExecutionAndPublication));

        try
        {
            return (T?)await pending.Value.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (pending.IsValueCreated && pending.Value.IsCompleted)
            {
                inFlight.TryRemove(new KeyValuePair<string, Lazy<Task<object?>>>(key, pending));
            }
        }
    }
}
