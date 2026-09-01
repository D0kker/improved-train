namespace LolAnalyzer.Application.Caching;

public sealed class CacheOptions
{
    public const string SectionName = "Cache";

    public int PlayerSummaryTtlSeconds { get; set; } = 300;

    public void Validate()
    {
        if (PlayerSummaryTtlSeconds is < 1 or > 86400)
        {
            throw new InvalidOperationException("Player summary cache TTL must be between 1 and 86400 seconds.");
        }
    }
}
