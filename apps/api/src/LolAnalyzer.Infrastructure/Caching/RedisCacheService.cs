using System.Text.Json;
using LolAnalyzer.Application.Caching;
using LolAnalyzer.Application.Observability;
using StackExchange.Redis;

namespace LolAnalyzer.Infrastructure.Caching;

public sealed class RedisCacheService(
    IRedisCacheStore redis,
    MemoryCacheService memory,
    OperationalMetrics metrics) : ICacheService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken) where T : class
    {
        try
        {
            var payload = await redis.GetAsync(key, cancellationToken).ConfigureAwait(false);
            if (payload is null)
            {
                memory.RemoveLocal(key);
                metrics.RecordCacheAccess(hit: false, fallback: false);
                return null;
            }

            var value = JsonSerializer.Deserialize<T>(payload, SerializerOptions);
            metrics.RecordCacheAccess(hit: value is not null, fallback: false);
            return value;
        }
        catch (Exception exception) when (IsRedisFailure(exception))
        {
            var value = await memory.GetAsync<T>(key, cancellationToken).ConfigureAwait(false);
            metrics.RecordCacheAccess(hit: value is not null, fallback: true);
            return value;
        }
        catch (JsonException)
        {
            memory.RemoveLocal(key);
            metrics.RecordCacheAccess(hit: false, fallback: false);
            return null;
        }
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan timeToLive,
        string tag,
        CancellationToken cancellationToken) where T : class
    {
        await memory.SetAsync(key, value, timeToLive, tag, cancellationToken).ConfigureAwait(false);
        try
        {
            var payload = JsonSerializer.Serialize(value, SerializerOptions);
            await redis.SetAsync(key, payload, timeToLive, tag, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (IsRedisFailure(exception))
        {
            // PostgreSQL remains the source of truth; the in-process cache is the safe fallback.
        }
    }

    public async Task RemoveTagAsync(string tag, CancellationToken cancellationToken)
    {
        await memory.RemoveTagAsync(tag, cancellationToken).ConfigureAwait(false);
        try
        {
            await redis.RemoveTagAsync(tag, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (IsRedisFailure(exception))
        {
            // A failed invalidation cannot corrupt PostgreSQL; cached values remain bounded by TTL.
        }
    }

    private static bool IsRedisFailure(Exception exception) =>
        exception is RedisException or TimeoutException or InvalidOperationException;
}
