using System.Collections.Concurrent;
using LolAnalyzer.Application.Caching;

namespace LolAnalyzer.Infrastructure.Caching;

public sealed class MemoryCacheService(TimeProvider timeProvider) : ICacheService
{
    private readonly ConcurrentDictionary<string, CacheEntry> entries = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> taggedKeys = new(StringComparer.Ordinal);

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken) where T : class
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!entries.TryGetValue(key, out var entry))
        {
            return Task.FromResult<T?>(null);
        }

        if (entry.ExpiresAt <= timeProvider.GetUtcNow())
        {
            entries.TryRemove(key, out _);
            return Task.FromResult<T?>(null);
        }

        return Task.FromResult(entry.Value as T);
    }

    public Task SetAsync<T>(
        string key,
        T value,
        TimeSpan timeToLive,
        string tag,
        CancellationToken cancellationToken) where T : class
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeToLive, TimeSpan.Zero);
        entries[key] = new CacheEntry(value, timeProvider.GetUtcNow().Add(timeToLive));
        taggedKeys.GetOrAdd(tag, _ => new ConcurrentDictionary<string, byte>(StringComparer.Ordinal))[key] = 0;
        return Task.CompletedTask;
    }

    public Task RemoveTagAsync(string tag, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (taggedKeys.TryRemove(tag, out var keys))
        {
            foreach (var key in keys.Keys)
            {
                entries.TryRemove(key, out _);
            }
        }

        return Task.CompletedTask;
    }

    public void RemoveLocal(string key) => entries.TryRemove(key, out _);

    private sealed record CacheEntry(object Value, DateTimeOffset ExpiresAt);
}
