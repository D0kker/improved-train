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

    public void FinishJob(TimeSpan duration, bool succeeded)
    {
        Interlocked.Decrement(ref activeJobCount);
        analysisDuration.Record(duration.TotalSeconds, new TagList { { "job.outcome", succeeded ? "completed" : "stopped" } });
        Interlocked.Increment(ref succeeded ? ref completedJobCount : ref failedJobCount);
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
        StoppedJobs: Interlocked.Read(ref failedJobCount));

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
    long StoppedJobs);
