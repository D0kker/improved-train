namespace LolAnalyzer.Application.Caching;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken) where T : class;

    Task SetAsync<T>(
        string key,
        T value,
        TimeSpan timeToLive,
        string tag,
        CancellationToken cancellationToken) where T : class;

    Task RemoveTagAsync(string tag, CancellationToken cancellationToken);
}
