using LolAnalyzer.Application.Observability;
using Xunit;

namespace LolAnalyzer.UnitTests;

public sealed class OperationalMetricsTests
{
    [Fact]
    public void SnapshotTracksOperationalSignalsWithoutIdentifiers()
    {
        using var metrics = new OperationalMetrics();

        metrics.RecordHttpRequest("GET", 200, TimeSpan.FromMilliseconds(12));
        metrics.RecordRiotRequest("match_ids", 429);
        metrics.RecordMatchesIngested(3);
        metrics.RecordCacheAccess(hit: true, fallback: true);
        metrics.StartJob();

        var running = metrics.Snapshot();
        Assert.Equal(1, running.ActiveJobs);

        metrics.FinishJob(TimeSpan.FromSeconds(2), succeeded: true);
        var result = metrics.Snapshot();

        Assert.Equal(1, result.HttpRequests);
        Assert.Equal(1, result.RiotRequests);
        Assert.Equal(1, result.RiotRateLimits);
        Assert.Equal(3, result.MatchesIngested);
        Assert.Equal(1, result.CacheHits);
        Assert.Equal(0, result.CacheMisses);
        Assert.Equal(1, result.CacheFallbacks);
        Assert.Equal(0, result.ActiveJobs);
        Assert.Equal(1, result.CompletedJobs);
        Assert.Equal(0, result.StoppedJobs);
    }
}
