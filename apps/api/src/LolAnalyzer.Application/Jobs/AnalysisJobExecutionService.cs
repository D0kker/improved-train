using LolAnalyzer.Application.Analysis;
using LolAnalyzer.Application.Caching;
using LolAnalyzer.Application.Matches;
using LolAnalyzer.Domain.Entities;

namespace LolAnalyzer.Application.Jobs;

public sealed class AnalysisJobExecutionService(
    IAnalysisJobRepository jobRepository,
    MatchIngestionService ingestionService,
    RepeatedPlayerAnalysisService repeatedPlayerAnalysisService,
    PlayerRelationshipAnalysisService relationshipAnalysisService,
    ICacheService cache,
    AnalysisJobExecutionOptions options,
    TimeProvider timeProvider)
{
    public async Task ExecuteAsync(
        AnalysisJob job,
        string platformRegion,
        CancellationToken cancellationToken)
    {
        var processed = Math.Clamp(job.MatchesProcessed, 0, job.RequestedCount);
        while (processed < job.RequestedCount)
        {
            if (await jobRepository.IsCancelledAsync(job.Id, cancellationToken).ConfigureAwait(false))
            {
                return;
            }

            var requestedBatch = Math.Min(options.BatchSize, job.RequestedCount - processed);
            var result = await ingestionService
                .SynchronizeAsync(job.Puuid, processed, requestedBatch, platformRegion, cancellationToken)
                .ConfigureAwait(false);
            processed += result.MatchIdsReturned;
            await jobRepository
                .UpdateProgressAsync(job.Id, processed, timeProvider.GetUtcNow(), cancellationToken)
                .ConfigureAwait(false);

            if (result.MatchIdsReturned < requestedBatch)
            {
                break;
            }
        }

        if (await jobRepository.IsCancelledAsync(job.Id, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        await repeatedPlayerAnalysisService.RebuildAsync(job.Puuid, cancellationToken).ConfigureAwait(false);
        await relationshipAnalysisService.RebuildAsync(cancellationToken).ConfigureAwait(false);
        await cache.RemoveTagAsync(PlayerCacheKeys.Tag(job.Puuid), cancellationToken).ConfigureAwait(false);
        await jobRepository
            .CompleteAsync(job.Id, processed, timeProvider.GetUtcNow(), cancellationToken)
            .ConfigureAwait(false);
    }
}
