namespace LolAnalyzer.Application.Caching;

public sealed class CacheOptions
{
    public const string SectionName = "Cache";

    public const string MemoryProvider = "memory";

    public const string RedisProvider = "redis";

    public string Provider { get; set; } = MemoryProvider;

    public int PlayerSummaryTtlSeconds { get; set; } = 300;

    public int RecentMatchIdsTtlSeconds { get; set; } = 120;

    public int MatchDetailsTtlSeconds { get; set; } = 3600;

    public int PlayerIdentityTtlSeconds { get; set; } = 1800;

    public int RankedTtlSeconds { get; set; } = 300;

    public int AnalysisResultTtlSeconds { get; set; } = 900;

    public int MaxMemoryEntries { get; set; } = 4096;

    public bool Enabled { get; set; } = true;

    public long MaxMemoryBytes { get; set; } = 64 * 1024 * 1024;

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

        if (RecentMatchIdsTtlSeconds is < 1 or > 86400
            || MatchDetailsTtlSeconds is < 1 or > 172800
            || PlayerIdentityTtlSeconds is < 1 or > 86400
            || AnalysisResultTtlSeconds is < 1 or > 86400)
        {
            throw new InvalidOperationException("Riot cache TTL values are outside the allowed range.");
        }

        if (RankedTtlSeconds is < 1 or > 900)
            throw new InvalidOperationException("Ranked cache TTL must be between 1 and 900 seconds.");

        if (MaxMemoryEntries is < 128 or > 100000)
        {
            throw new InvalidOperationException("MaxMemoryEntries must be between 128 and 100000.");
        }

        if (MaxMemoryBytes is < 1048576 or > 1073741824)
            throw new InvalidOperationException("Cache byte limit must be between 1 MiB and 1 GiB.");
    }
}
