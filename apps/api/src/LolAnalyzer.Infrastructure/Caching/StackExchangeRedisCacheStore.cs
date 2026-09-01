using StackExchange.Redis;

namespace LolAnalyzer.Infrastructure.Caching;

public sealed class StackExchangeRedisCacheStore(IConnectionMultiplexer connection) : IRedisCacheStore
{
    private const string TagPrefix = "cache-tag:";

    public async Task<string?> GetAsync(string key, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var value = await connection.GetDatabase().StringGetAsync(key).WaitAsync(cancellationToken).ConfigureAwait(false);
        return value.HasValue ? value.ToString() : null;
    }

    public async Task SetAsync(
        string key,
        string payload,
        TimeSpan timeToLive,
        string tag,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var database = connection.GetDatabase();
        var tagKey = TagPrefix + tag;
        await database.StringSetAsync(key, payload, timeToLive).WaitAsync(cancellationToken).ConfigureAwait(false);
        await database.SetAddAsync(tagKey, key).WaitAsync(cancellationToken).ConfigureAwait(false);
        await database.KeyExpireAsync(tagKey, timeToLive).WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task RemoveTagAsync(string tag, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var database = connection.GetDatabase();
        var tagKey = TagPrefix + tag;
        var keys = await database.SetMembersAsync(tagKey).WaitAsync(cancellationToken).ConfigureAwait(false);
        if (keys.Length > 0)
        {
            await database.KeyDeleteAsync(keys.Select(value => (RedisKey)value.ToString()).ToArray())
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        await database.KeyDeleteAsync(tagKey).WaitAsync(cancellationToken).ConfigureAwait(false);
    }
}
