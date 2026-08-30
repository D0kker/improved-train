using LolAnalyzer.Application.Riot;

namespace LolAnalyzer.Application.Matches;

public sealed class MatchIngestionService(
    IRiotApiClient riotApiClient,
    IMatchRepository matchRepository,
    MatchIngestionOptions options)
{
    public async Task<MatchSyncResult> SynchronizeAsync(
        string puuid,
        int count,
        string platformRegion,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(puuid);
        ArgumentException.ThrowIfNullOrWhiteSpace(platformRegion);
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, 100);
        if (options.RequestConcurrency is < 1 or > 5)
        {
            throw new InvalidOperationException("Match request concurrency must be between 1 and 5.");
        }

        var matchIds = (await riotApiClient
                .GetMatchIdsAsync(puuid, start: 0, count: count, cancellationToken: cancellationToken)
                .ConfigureAwait(false))
            .Where(matchId => !string.IsNullOrWhiteSpace(matchId))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var existingMatchIds = await matchRepository
            .FindExistingRiotMatchIdsAsync(matchIds, cancellationToken)
            .ConfigureAwait(false);
        var missingMatchIds = matchIds.Where(matchId => !existingMatchIds.Contains(matchId)).ToArray();

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = options.RequestConcurrency,
            CancellationToken = cancellationToken,
        };
        var downloadedMatches = new RiotMatchData?[missingMatchIds.Length];

        await Parallel.ForEachAsync(
            Enumerable.Range(0, missingMatchIds.Length),
            parallelOptions,
            async (index, token) =>
            {
                downloadedMatches[index] = await riotApiClient
                    .GetMatchAsync(missingMatchIds[index], token)
                    .ConfigureAwait(false);
            }).ConfigureAwait(false);

        var persistedCount = 0;
        var notFoundCount = 0;
        foreach (var downloadedMatch in downloadedMatches)
        {
            if (downloadedMatch is null)
            {
                notFoundCount++;
                continue;
            }

            if (await matchRepository
                    .SaveIfMissingAsync(downloadedMatch, platformRegion, cancellationToken)
                    .ConfigureAwait(false))
            {
                persistedCount++;
            }
        }

        return new MatchSyncResult(
            RequestedCount: count,
            MatchIdsReturned: matchIds.Length,
            AlreadyStored: existingMatchIds.Count,
            Downloaded: downloadedMatches.Count(match => match is not null),
            Persisted: persistedCount,
            NotFound: notFoundCount);
    }
}
