using LolAnalyzer.Application.Caching;

namespace LolAnalyzer.Application.Analysis;

public sealed class PlayerSummaryQueryService(
    IPlayerAnalysisRepository repository,
    ICacheService cache,
    CacheOptions options)
{
    public async Task<PlayerSummary?> GetAsync(string puuid, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(puuid);
        var key = PlayerCacheKeys.Summary(puuid);
        var cached = await cache.GetAsync<PlayerSummary>(key, cancellationToken).ConfigureAwait(false);
        if (cached is not null)
        {
            return cached;
        }

        var summary = await repository.GetSummaryAsync(puuid, cancellationToken).ConfigureAwait(false);
        if (summary is not null)
        {
            await cache.SetAsync(
                key,
                summary,
                TimeSpan.FromSeconds(options.PlayerSummaryTtlSeconds),
                PlayerCacheKeys.Tag(puuid),
                cancellationToken).ConfigureAwait(false);
        }

        return summary;
    }
}
