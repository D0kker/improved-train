using System.Text.Json;
using LolAnalyzer.Application.Caching;

namespace LolAnalyzer.Infrastructure.Caching;

/// <summary>Optional, bounded response cache. Contains no authoritative analysis state.</summary>
public sealed class MemoryCacheService(TimeProvider timeProvider, CacheOptions? options = null) : ICacheService
{
    private readonly CacheOptions settings = options ?? new CacheOptions();
    private readonly object gate = new();
    private readonly Dictionary<string, CacheEntry> entries = new(StringComparer.Ordinal);
    private long bytes;

    public int Count { get { lock (gate) return entries.Count; } }
    public long EstimatedBytes { get { lock (gate) return bytes; } }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken) where T : class
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (gate)
        {
            if (!settings.Enabled || !entries.TryGetValue(key, out var entry)) return Task.FromResult<T?>(null);
            if (entry.ExpiresAt > timeProvider.GetUtcNow()) return Task.FromResult(entry.Value as T);
            Remove(key);
            return Task.FromResult<T?>(null);
        }
    }

    public Task SetAsync<T>(string key, T value, TimeSpan timeToLive, string tag, CancellationToken cancellationToken) where T : class
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeToLive, TimeSpan.Zero);
        if (!settings.Enabled) return Task.CompletedTask;
        // Conservative budget for UTF-16 strings, object/collection overhead and index metadata.
        long size = 1024 + key.Length * 2L + tag.Length * 2L + JsonSerializer.SerializeToUtf8Bytes(value).LongLength * 4;
        if (size > settings.MaxMemoryBytes) return Task.CompletedTask;
        lock (gate)
        {
            foreach (var expired in entries.Where(pair => pair.Value.ExpiresAt <= timeProvider.GetUtcNow()).Select(pair => pair.Key).ToArray()) Remove(expired);
            Remove(key);
            while (entries.Count > 0 && (entries.Count >= settings.MaxMemoryEntries || bytes + size > settings.MaxMemoryBytes)) Remove(entries.Keys.First());
            entries[key] = new CacheEntry(value, timeProvider.GetUtcNow().Add(timeToLive), tag, size);
            bytes += size;
        }
        return Task.CompletedTask;
    }

    public Task RemoveTagAsync(string tag, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (gate)
            foreach (var key in entries.Where(pair => pair.Value.Tag == tag).Select(pair => pair.Key).ToArray()) Remove(key);
        return Task.CompletedTask;
    }

    public void RemoveLocal(string key) { lock (gate) Remove(key); }

    private void Remove(string key) { if (entries.Remove(key, out var removed)) bytes -= removed.Size; }

    private sealed record CacheEntry(object Value, DateTimeOffset ExpiresAt, string Tag, long Size);
}
