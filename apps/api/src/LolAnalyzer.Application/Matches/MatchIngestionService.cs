using LolAnalyzer.Application.Riot;
using LolAnalyzer.Application.Observability;
using System.Diagnostics;

namespace LolAnalyzer.Application.Matches;

public sealed class MatchIngestionService(
    IRiotApiClient riotApiClient,
    IMatchRepository matchRepository,
    MatchIngestionOptions options,
    OperationalMetrics metrics)
{
    public async Task<MatchSyncResult> SynchronizeAsync(
        string puuid,
        int count,
        string platformRegion,
        CancellationToken cancellationToken,
        string? excludedRiotMatchId = null) =>
        await SynchronizeAsync(puuid, start: 0, count, platformRegion, cancellationToken, excludedRiotMatchId).ConfigureAwait(false);

    public async Task<MatchSyncResult> SynchronizeAsync(
        string puuid,
        int start,
        int count,
        string platformRegion,
        CancellationToken cancellationToken,
        string? excludedRiotMatchId = null)
    {
        var stopwatch = Stopwatch.StartNew();
        ArgumentException.ThrowIfNullOrWhiteSpace(puuid);
        ArgumentException.ThrowIfNullOrWhiteSpace(platformRegion);
        ArgumentOutOfRangeException.ThrowIfNegative(start);
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, 100);
        if (options.RequestConcurrency is < 1 or > 5)
        {
            throw new InvalidOperationException("Match request concurrency must be between 1 and 5.");
        }

        var requestedFromRiot = string.IsNullOrWhiteSpace(excludedRiotMatchId)
            ? count
            : Math.Min(100, count + 1);
        var matchIds = (await riotApiClient
                .GetMatchIdsAsync(puuid, start, requestedFromRiot, cancellationToken)
                .ConfigureAwait(false))
            .Where(matchId => !string.IsNullOrWhiteSpace(matchId))
            .Where(matchId => !string.Equals(matchId, excludedRiotMatchId, StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .Take(count)
            .ToArray();
        var existingMatchIds = await matchRepository
            .FindExistingRiotMatchIdsAsync(matchIds, cancellationToken)
            .ConfigureAwait(false);
        var missingMatchIds = matchIds.Where(matchId => !existingMatchIds.Contains(matchId)).ToArray();
        metrics.RecordMatchAnalysisPlan(matchIds.Length, matchIds.Length, existingMatchIds.Count);

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = options.RequestConcurrency,
            CancellationToken = cancellationToken,
        };
        var downloadedMatches = new RiotMatchData?[missingMatchIds.Length];
        var failedDownloads = 0;

        await Parallel.ForEachAsync(
            Enumerable.Range(0, missingMatchIds.Length),
            parallelOptions,
            async (index, token) =>
            {
                try
                {
                    downloadedMatches[index] = await riotApiClient
                        .GetMatchAsync(missingMatchIds[index], token)
                        .ConfigureAwait(false);
                }
                catch (RiotApiException)
                {
                    Interlocked.Increment(ref failedDownloads);
                }
                catch (HttpRequestException) when (!token.IsCancellationRequested)
                {
                    Interlocked.Increment(ref failedDownloads);
                }
                catch (TaskCanceledException) when (!token.IsCancellationRequested)
                {
                    Interlocked.Increment(ref failedDownloads);
                }
            }).ConfigureAwait(false);

        var persistedCount = 0;
        var persistedMatchIds = new List<string>();
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
                persistedMatchIds.Add(downloadedMatch.RiotMatchId);
            }
        }

        metrics.RecordMatchesIngested(persistedCount);
        metrics.RecordSyncDuration(stopwatch.Elapsed, failedDownloads == 0);
        return new MatchSyncResult(
            RequestedCount: count,
            MatchIdsReturned: matchIds.Length,
            AlreadyStored: existingMatchIds.Count,
            Downloaded: downloadedMatches.Count(match => match is not null),
            Persisted: persistedCount,
            NotFound: notFoundCount,
            Failed: failedDownloads,
            PersistedMatchIds: persistedMatchIds);
    }
}
