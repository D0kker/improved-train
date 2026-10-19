using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace LolAnalyzer.Application.Observability;

public sealed class OperationalMetrics : IDisposable
{
    public const string MeterName = "LolAnalyzer.Operations";

    private readonly Meter meter = new(MeterName);
    private readonly Counter<long> httpRequests;
    private readonly Histogram<double> httpDuration;
    private readonly Counter<long> riotRequests;
    private readonly Counter<long> riotRateLimits;
    private readonly Counter<long> matchesIngested;
    private readonly Counter<long> cacheHits;
    private readonly Counter<long> cacheMisses;
    private readonly Counter<long> cacheFallbacks;
    private readonly Histogram<double> analysisDuration;
    private readonly Counter<long> echoMatchesAnalyzed;
    private readonly Counter<long> echoEncounterLookups;
    private readonly Counter<long> echoEncounterHits;
    private readonly Counter<long> echoKnownPlayersFound;
    private long httpRequestCount;
    private long riotRequestCount;
    private long riotRateLimitCount;
    private long matchesIngestedCount;
    private long cacheHitCount;
    private long cacheMissCount;
    private long cacheFallbackCount;
    private long completedJobCount;
    private long failedJobCount;
    private long activeJobCount;
    private long echoMatchesAnalyzedCount;
    private long echoEncounterLookupCount;
    private long echoEncounterHitCount;
    private long echoKnownPlayersFoundCount;
    private long matchIdsReturnedCount;
    private long uniqueMatchIdsCount;
    private long avoidedMatchDetailRequestCount;

    public OperationalMetrics()
    {
        httpRequests = meter.CreateCounter<long>("http.server.requests", unit: "{request}");
        httpDuration = meter.CreateHistogram<double>("http.server.duration", unit: "s");
        riotRequests = meter.CreateCounter<long>("riot.api.requests", unit: "{request}");
        riotRateLimits = meter.CreateCounter<long>("riot.api.rate_limits", unit: "{response}");
        matchesIngested = meter.CreateCounter<long>("matches.ingested", unit: "{match}");
        cacheHits = meter.CreateCounter<long>("cache.hits", unit: "{lookup}");
        cacheMisses = meter.CreateCounter<long>("cache.misses", unit: "{lookup}");
        cacheFallbacks = meter.CreateCounter<long>("cache.fallbacks", unit: "{operation}");
        analysisDuration = meter.CreateHistogram<double>("analysis.duration", unit: "s");
        echoMatchesAnalyzed = meter.CreateCounter<long>("echo_matches_analyzed_total", unit: "{match}");
        echoEncounterLookups = meter.CreateCounter<long>("echo_encounter_lookups_total", unit: "{lookup}");
        echoEncounterHits = meter.CreateCounter<long>("echo_encounter_hits_total", unit: "{match}");
        echoKnownPlayersFound = meter.CreateCounter<long>("echo_known_players_found_total", unit: "{player}");
        meter.CreateObservableGauge("analysis.jobs.active", () => Interlocked.Read(ref activeJobCount), unit: "{job}");
    }

    public void RecordHttpRequest(string method, int statusCode, TimeSpan duration)
    {
        Interlocked.Increment(ref httpRequestCount);
        var tags = new TagList { { "http.request.method", method }, { "http.response.status_code", statusCode } };
        httpRequests.Add(1, tags);
        httpDuration.Record(duration.TotalSeconds, tags);
    }

    public void RecordRiotRequest(string endpoint, int statusCode)
    {
        Interlocked.Increment(ref riotRequestCount);
        riotRequests.Add(1, new TagList { { "riot.endpoint", endpoint }, { "http.response.status_code", statusCode } });
        if (statusCode == 429)
        {
            Interlocked.Increment(ref riotRateLimitCount);
            riotRateLimits.Add(1, new TagList { { "riot.endpoint", endpoint } });
        }
    }

    public void RecordMatchesIngested(int count)
    {
        if (count <= 0)
        {
            return;
        }

        Interlocked.Add(ref matchesIngestedCount, count);
        matchesIngested.Add(count);
    }

    public void RecordMatchAnalysisPlan(int matchIdsReturned, int uniqueMatchIds, int avoidedDetailRequests)
    {
        if (matchIdsReturned < 0 || uniqueMatchIds < 0 || avoidedDetailRequests < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(matchIdsReturned));
        }

        Interlocked.Add(ref matchIdsReturnedCount, matchIdsReturned);
        Interlocked.Add(ref uniqueMatchIdsCount, uniqueMatchIds);
        Interlocked.Add(ref avoidedMatchDetailRequestCount, avoidedDetailRequests);
    }

    public void RecordCacheAccess(bool hit, bool fallback)
    {
        if (hit)
        {
            Interlocked.Increment(ref cacheHitCount);
            cacheHits.Add(1);
        }
        else
        {
            Interlocked.Increment(ref cacheMissCount);
            cacheMisses.Add(1);
        }

        if (fallback)
        {
            Interlocked.Increment(ref cacheFallbackCount);
            cacheFallbacks.Add(1);
        }
    }

    public void StartJob() => Interlocked.Increment(ref activeJobCount);

    public void RecordMatchFamiliarity(int knownPlayers)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(knownPlayers);

        Interlocked.Increment(ref echoMatchesAnalyzedCount);
        Interlocked.Increment(ref echoEncounterLookupCount);
        echoMatchesAnalyzed.Add(1);
        echoEncounterLookups.Add(1);

        if (knownPlayers == 0)
        {
            return;
        }

        Interlocked.Increment(ref echoEncounterHitCount);
        Interlocked.Add(ref echoKnownPlayersFoundCount, knownPlayers);
        echoEncounterHits.Add(1);
        echoKnownPlayersFound.Add(knownPlayers);
    }

    public void FinishJob(TimeSpan duration, bool succeeded)
    {
        Interlocked.Decrement(ref activeJobCount);
        analysisDuration.Record(duration.TotalSeconds, new TagList { { "job.outcome", succeeded ? "completed" : "stopped" } });
        Interlocked.Increment(ref succeeded ? ref completedJobCount : ref failedJobCount);
    }

    public void RecordSyncDuration(TimeSpan duration, bool completed)
    {
        analysisDuration.Record(
            duration.TotalSeconds,
            new TagList { { "job.outcome", completed ? "completed" : "partial" }, { "analysis.kind", "match-sync" } });
    }

    public OperationalMetricsSnapshot Snapshot() => new(
        HttpRequests: Interlocked.Read(ref httpRequestCount),
        RiotRequests: Interlocked.Read(ref riotRequestCount),
        RiotRateLimits: Interlocked.Read(ref riotRateLimitCount),
        MatchesIngested: Interlocked.Read(ref matchesIngestedCount),
        CacheHits: Interlocked.Read(ref cacheHitCount),
        CacheMisses: Interlocked.Read(ref cacheMissCount),
        CacheFallbacks: Interlocked.Read(ref cacheFallbackCount),
        ActiveJobs: Interlocked.Read(ref activeJobCount),
        CompletedJobs: Interlocked.Read(ref completedJobCount),
        StoppedJobs: Interlocked.Read(ref failedJobCount),
        EchoMatchesAnalyzed: Interlocked.Read(ref echoMatchesAnalyzedCount),
        EchoEncounterLookups: Interlocked.Read(ref echoEncounterLookupCount),
        EchoEncounterHits: Interlocked.Read(ref echoEncounterHitCount),
        EchoKnownPlayersFound: Interlocked.Read(ref echoKnownPlayersFoundCount),
        MatchIdsReturned: Interlocked.Read(ref matchIdsReturnedCount),
        UniqueMatchIds: Interlocked.Read(ref uniqueMatchIdsCount),
        AvoidedMatchDetailRequests: Interlocked.Read(ref avoidedMatchDetailRequestCount));

    public void Dispose() => meter.Dispose();
}

public sealed record OperationalMetricsSnapshot(
    long HttpRequests,
    long RiotRequests,
    long RiotRateLimits,
    long MatchesIngested,
    long CacheHits,
    long CacheMisses,
    long CacheFallbacks,
    long ActiveJobs,
    long CompletedJobs,
    long StoppedJobs,
    long EchoMatchesAnalyzed,
    long EchoEncounterLookups,
    long EchoEncounterHits,
    long EchoKnownPlayersFound,
    long MatchIdsReturned,
    long UniqueMatchIds,
    long AvoidedMatchDetailRequests)
{
    public decimal EchoEncounterHitRate => EchoMatchesAnalyzed == 0
        ? 0
        : Math.Round(EchoEncounterHits * 100m / EchoMatchesAnalyzed, 1, MidpointRounding.AwayFromZero);
}
