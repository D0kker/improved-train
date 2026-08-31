using LolAnalyzer.Domain.Entities;

namespace LolAnalyzer.Application.Jobs;

public sealed class AnalysisJobService(IAnalysisJobRepository repository, TimeProvider timeProvider)
{
    public async Task<AnalysisJobView> StartAsync(
        string puuid,
        int requestedCount,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var job = new AnalysisJob
        {
            Puuid = puuid,
            RequestedCount = requestedCount,
            Status = AnalysisJobStatus.Queued,
            CreatedAt = now,
            UpdatedAt = now,
        };

        await repository.AddAsync(job, cancellationToken).ConfigureAwait(false);
        return Map(job);
    }

    public async Task<AnalysisJobView?> FindAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await repository.FindAsync(jobId, cancellationToken).ConfigureAwait(false);
        return job is null ? null : Map(job);
    }

    private static AnalysisJobView Map(AnalysisJob job) =>
        new(
            job.Id,
            job.Puuid,
            job.RequestedCount,
            job.Status.ToString().ToLowerInvariant(),
            job.MatchesProcessed,
            job.ErrorCode,
            job.CreatedAt,
            job.UpdatedAt,
            job.StartedAt,
            job.CompletedAt);
}

public sealed record AnalysisJobView(
    Guid JobId,
    string Puuid,
    int MatchesRequested,
    string Status,
    int MatchesProcessed,
    string? ErrorCode,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt);
