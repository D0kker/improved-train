using System.Diagnostics;
using System.Net;
using System.Text.Json;
using LolAnalyzer.Application.Analysis;
using LolAnalyzer.Application.Observability;
using LolAnalyzer.Application.Riot;

namespace LolAnalyzer.Application.Stateless;

public sealed class OnDemandAnalysisService(
    IRiotApiClient riot,
    RequestAnalysisCalculator calculator,
    OnDemandAnalysisOptions options,
    OperationalMetrics metrics)
{
    public async Task<PlayerSearchResult?> SearchAsync(string gameName, string tagLine, string platformRegion, CancellationToken cancellationToken)
    {
        var client = riot.ForPlatform(platformRegion);
        var account = await client.GetAccountByRiotIdAsync(gameName, tagLine, cancellationToken).ConfigureAwait(false);
        if (account is null) return null;
        return await RecentAsync(account.Puuid, platformRegion, 0, 5, cancellationToken,
            new PlayerIdentity(account.Puuid, account.GameName, account.TagLine, platformRegion)).ConfigureAwait(false);
    }

    public async Task<PlayerSearchResult> RecentAsync(string puuid, string platformRegion, int start, int count,
        CancellationToken cancellationToken, PlayerIdentity? identity = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(puuid);
        ArgumentOutOfRangeException.ThrowIfNegative(start);
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, 5);
        var client = riot.ForPlatform(platformRegion);
        var ids = (await client.GetMatchIdsAsync(puuid, start, count, cancellationToken).ConfigureAwait(false))
            .Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.Ordinal).Take(count).ToArray();
        var downloads = await DownloadAsync(client, ids, cancellationToken).ConfigureAwait(false);
        var matches = downloads.Where(item => item.Match is not null && item.Match.Participants.Any(player => player.Puuid == puuid))
            .Select(item => item.Match!).OrderByDescending(RequestAnalysisCalculator.OccurredAt)
            .ThenBy(match => match.RiotMatchId, StringComparer.Ordinal).ToArray();
        var player = matches.SelectMany(match => match.Participants).FirstOrDefault(candidate => candidate.Puuid == puuid);
        identity ??= new PlayerIdentity(puuid, player?.GameName ?? "", player?.TagLine ?? "", platformRegion);
        var failures = downloads.Where(item => item.Failure is not null).Select(item => item.Failure!).ToArray();
        var limited = matches.Length < count;
        var coverage = new AnalysisCoverage(failures.Length > 0 ? matches.Length == 0 ? "error" : "partial"
            : limited ? "insufficient" : "complete", 1, ids.Length, ids.Length, matches.Length,
            [new ParticipantCoverage(puuid, count, ids.Length, matches.Length, limited)], failures);
        return new PlayerSearchResult(identity, RequestAnalysisCalculator.Summary(identity, matches),
            new PagedPlayerMatches(start / count + 1, count, start + matches.Length, matches.Select(match =>
            {
                var own = match.Participants.First(candidate => candidate.Puuid == puuid);
                return new PlayerMatchListItem(match.RiotMatchId, match.QueueId, match.GameStartTimestamp, match.GameDurationSeconds,
                    own.ChampionId, own.ChampionName, own.Kills, own.Deaths, own.Assists, own.Win);
            }).ToArray()), RequestAnalysisCalculator.Encounters(puuid, matches), coverage);
    }

    public async Task<MatchAnalysisResult?> AnalyzeAsync(string matchId, string ownerPuuid, string platformRegion, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(matchId);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerPuuid);
        if (matchId.Length > 64 || ownerPuuid.Length > 128) throw new ArgumentException("Invalid match or player identifier.");
        var started = Stopwatch.GetTimestamp();
        var client = riot.ForPlatform(platformRegion);
        var target = await client.GetMatchAsync(matchId, cancellationToken).ConfigureAwait(false);
        if (target is null) return null;
        if (target.RiotMatchId != matchId || target.Participants.Count > 10)
            throw new ArgumentException("The selected match is not a supported ten-player match.");
        var participants = target.Participants.Where(player => !string.IsNullOrWhiteSpace(player.Puuid))
            .DistinctBy(player => player.Puuid).OrderBy(player => player.Puuid, StringComparer.Ordinal).ToArray();
        if (!participants.Any(player => player.Puuid == ownerPuuid)) throw new ArgumentException("Owner is not present in the selected match.");
        var targetStart = target.GameStartTimestamp ?? target.GameCreation;
        if (targetStart is null)
            return calculator.Analyze(target, ownerPuuid, platformRegion, [], new AnalysisCoverage("insufficient",
                participants.Length, 0, 0, 0, [], [new AnalysisFailure("target", "missing_target_timestamp")]));
        var plans = new HistoryPlan[participants.Length];
        await Parallel.ForEachAsync(Enumerable.Range(0, participants.Length), ParallelSettings(cancellationToken), async (index, token) =>
        {
            var puuid = participants[index].Puuid;
            var count = puuid == ownerPuuid ? 5 : 3;
            try
            {
                // endTime is seconds; fetching one extra compensates inclusive boundaries/target exclusion.
                var ids = await client.GetMatchIdsBeforeAsync(puuid, count + 1,
                    targetStart.Value.ToUnixTimeSeconds(), token).ConfigureAwait(false);
                plans[index] = new HistoryPlan(puuid, count, ids.Where(id => !string.IsNullOrWhiteSpace(id) && id != matchId)
                    .Distinct(StringComparer.Ordinal).Take(count).ToArray(), null);
            }
            catch (Exception error) when (IsRiotFailure(error, token))
            {
                plans[index] = new HistoryPlan(puuid, count, [], new AnalysisFailure("history_ids", ErrorCode(error), puuid));
            }
        }).ConfigureAwait(false);
        var allIds = plans.SelectMany(plan => plan.Ids).ToArray();
        var uniqueIds = allIds.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        var downloaded = await DownloadAsync(client, uniqueIds, cancellationToken).ConfigureAwait(false);
        var recovered = downloaded.Where(item => item.Match is not null).ToDictionary(item => item.Id, item => item.Match!, StringComparer.Ordinal);
        var selected = new Dictionary<string, RiotMatchData>(StringComparer.Ordinal);
        var coverageRows = plans.Select(plan =>
        {
            var eligible = plan.Ids.Where(recovered.ContainsKey).Select(id => recovered[id])
                .Where(match => match.RiotMatchId != matchId && match.GameEndTimestamp <= targetStart
                    && match.Participants.Any(player => player.Puuid == plan.Puuid))
                .OrderByDescending(RequestAnalysisCalculator.OccurredAt).ThenBy(match => match.RiotMatchId, StringComparer.Ordinal)
                .Take(plan.Count).ToArray();
            foreach (var match in eligible) selected.TryAdd(match.RiotMatchId, match);
            return new ParticipantCoverage(plan.Puuid, plan.Count, plan.Ids.Length, eligible.Length, eligible.Length < plan.Count);
        }).ToArray();
        var failures = plans.Where(plan => plan.Failure is not null).Select(plan => plan.Failure!)
            .Concat(downloaded.Where(item => item.Failure is not null).Select(item => item.Failure!)).ToArray();
        var status = failures.Length > 0 ? selected.Count == 0 ? "error" : "partial"
            : selected.Count == 0 ? "insufficient"
            : participants.Length != 10 || coverageRows.Any(row => row.Limited) ? "partial" : "complete";
        var coverage = new AnalysisCoverage(status, participants.Length, allIds.Length, uniqueIds.Length, selected.Count, coverageRows, failures);
        cancellationToken.ThrowIfCancellationRequested();
        var result = calculator.Analyze(target, ownerPuuid, platformRegion, selected.Values.ToArray(), coverage);
        metrics.RecordMatchAnalysisPlan(allIds.Length, uniqueIds.Length, allIds.Length - uniqueIds.Length);
        metrics.RecordSyncDuration(Stopwatch.GetElapsedTime(started), status == "complete");
        if (status == "complete") metrics.RecordMatchFamiliarity(result.Match.Familiarity?.KnownPlayers ?? 0);
        return result;
    }

    private async Task<Download[]> DownloadAsync(IRiotApiClient client, string[] ids, CancellationToken cancellationToken)
    {
        var results = new Download[ids.Length];
        await Parallel.ForEachAsync(Enumerable.Range(0, ids.Length), ParallelSettings(cancellationToken), async (index, token) =>
        {
            try
            {
                var match = await client.GetMatchAsync(ids[index], token).ConfigureAwait(false);
                results[index] = match is not null && match.RiotMatchId == ids[index]
                    ? new Download(ids[index], match with { RawJson = "" }, null)
                    : new Download(ids[index], null, new AnalysisFailure("match_detail", "match_unavailable", MatchId: ids[index]));
            }
            catch (Exception error) when (IsRiotFailure(error, token))
            {
                results[index] = new Download(ids[index], null, new AnalysisFailure("match_detail", ErrorCode(error), MatchId: ids[index]));
            }
        }).ConfigureAwait(false);
        return results;
    }

    private ParallelOptions ParallelSettings(CancellationToken token) => new() { MaxDegreeOfParallelism = options.RequestConcurrency, CancellationToken = token };

    private static bool IsRiotFailure(Exception error, CancellationToken token) => !token.IsCancellationRequested
        && error is RiotApiException or HttpRequestException or OperationCanceledException or JsonException;

    private static string ErrorCode(Exception error) => error is RiotApiException { StatusCode: HttpStatusCode.TooManyRequests }
        ? "riot_rate_limited" : "riot_unavailable";

    private sealed record HistoryPlan(string Puuid, int Count, string[] Ids, AnalysisFailure? Failure);
    private sealed record Download(string Id, RiotMatchData? Match, AnalysisFailure? Failure);
}
