namespace LolAnalyzer.Application.Riot;

public interface IRiotResponseCache
{
    Task<T?> GetOrCreateAsync<T>(
        string key,
        TimeSpan timeToLive,
        string tag,
        Func<CancellationToken, Task<T?>> factory,
        CancellationToken cancellationToken)
        where T : class;
}
