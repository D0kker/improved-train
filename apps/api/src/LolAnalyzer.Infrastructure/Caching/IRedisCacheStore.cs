namespace LolAnalyzer.Infrastructure.Caching;

public interface IRedisCacheStore
{
    Task<string?> GetAsync(string key, CancellationToken cancellationToken);

    Task SetAsync(
        string key,
        string payload,
        TimeSpan timeToLive,
        string tag,
        CancellationToken cancellationToken);

    Task RemoveTagAsync(string tag, CancellationToken cancellationToken);
}
