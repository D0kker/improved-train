namespace LolAnalyzer.Application.Caching;

public sealed class CacheOptions
{
    public const string SectionName = "Cache";

    public const string MemoryProvider = "memory";

    public const string RedisProvider = "redis";

    public string Provider { get; set; } = MemoryProvider;

    public int PlayerSummaryTtlSeconds { get; set; } = 300;

    public bool UsesRedis => string.Equals(Provider, RedisProvider, StringComparison.OrdinalIgnoreCase);

    public void Validate()
    {
        Provider = Provider?.Trim().ToLowerInvariant() ?? string.Empty;
        if (Provider is not MemoryProvider and not RedisProvider)
        {
            throw new InvalidOperationException("Cache provider must be 'memory' or 'redis'.");
        }

        if (PlayerSummaryTtlSeconds is < 1 or > 86400)
        {
            throw new InvalidOperationException("Player summary cache TTL must be between 1 and 86400 seconds.");
        }
    }
}
