using LolAnalyzer.Application.Jobs;
using LolAnalyzer.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LolAnalyzer.Infrastructure.Persistence;

public sealed class AnalysisJobRepository(LolAnalyzerDbContext dbContext) : IAnalysisJobRepository
{
    public async Task<AnalysisJob> AddOrGetActiveAsync(AnalysisJob job, CancellationToken cancellationToken)
    {
        var activeJob = await FindActiveEquivalentAsync(job, cancellationToken).ConfigureAwait(false);
        if (activeJob is not null)
        {
            return activeJob;
        }

        dbContext.AnalysisJobs.Add(job);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return job;
        }
        catch (DbUpdateException)
        {
            dbContext.Entry(job).State = EntityState.Detached;
            var existingJob = await FindActiveEquivalentAsync(job, cancellationToken).ConfigureAwait(false);
            if (existingJob is null)
            {
                throw;
            }

            return existingJob;
        }
    }

    public Task<AnalysisJob?> FindAsync(Guid jobId, CancellationToken cancellationToken) =>
        dbContext.AnalysisJobs
            .AsNoTracking()
            .SingleOrDefaultAsync(job => job.Id == jobId, cancellationToken);

    public async Task<AnalysisJob?> ClaimNextAsync(
        DateTimeOffset now,
        DateTimeOffset staleBefore,
        CancellationToken cancellationToken)
    {
        var job = await dbContext.AnalysisJobs
            .Where(candidate => candidate.Status == AnalysisJobStatus.Queued
                || (candidate.Status == AnalysisJobStatus.Running && candidate.UpdatedAt < staleBefore))
            .OrderBy(candidate => candidate.Status == AnalysisJobStatus.Running ? 0 : 1)
            .ThenBy(candidate => candidate.CreatedAt)
            .ThenBy(candidate => candidate.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (job is null)
        {
            return null;
        }

        job.Status = AnalysisJobStatus.Running;
        job.StartedAt ??= now;
        job.UpdatedAt = now;
        job.ErrorCode = null;
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return job;
    }

    public Task UpdateProgressAsync(
        Guid jobId,
        int matchesProcessed,
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        dbContext.AnalysisJobs
            .Where(job => job.Id == jobId && job.Status == AnalysisJobStatus.Running)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(job => job.MatchesProcessed, matchesProcessed)
                    .SetProperty(job => job.UpdatedAt, now),
                cancellationToken);

    public Task CompleteAsync(
        Guid jobId,
        int matchesProcessed,
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        dbContext.AnalysisJobs
            .Where(job => job.Id == jobId && job.Status == AnalysisJobStatus.Running)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(job => job.Status, AnalysisJobStatus.Completed)
                    .SetProperty(job => job.MatchesProcessed, matchesProcessed)
                    .SetProperty(job => job.ErrorCode, (string?)null)
                    .SetProperty(job => job.UpdatedAt, now)
                    .SetProperty(job => job.CompletedAt, now),
                cancellationToken);

    public Task FailAsync(
        Guid jobId,
        string errorCode,
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        dbContext.AnalysisJobs
            .Where(job => job.Id == jobId && job.Status == AnalysisJobStatus.Running)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(job => job.Status, AnalysisJobStatus.Failed)
                    .SetProperty(job => job.ErrorCode, errorCode)
                    .SetProperty(job => job.UpdatedAt, now)
                    .SetProperty(job => job.CompletedAt, now),
                cancellationToken);

    public Task RequeueAsync(
        Guid jobId,
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        dbContext.AnalysisJobs
            .Where(job => job.Id == jobId && job.Status == AnalysisJobStatus.Running)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(job => job.Status, AnalysisJobStatus.Queued)
                    .SetProperty(job => job.UpdatedAt, now),
                cancellationToken);

    public async Task<AnalysisJob?> CancelAsync(
        Guid jobId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await dbContext.AnalysisJobs
            .Where(job => job.Id == jobId
                && (job.Status == AnalysisJobStatus.Queued || job.Status == AnalysisJobStatus.Running))
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(job => job.Status, AnalysisJobStatus.Cancelled)
                    .SetProperty(job => job.ErrorCode, (string?)null)
                    .SetProperty(job => job.UpdatedAt, now)
                    .SetProperty(job => job.CompletedAt, now),
                cancellationToken)
            .ConfigureAwait(false);
        return await FindAsync(jobId, cancellationToken).ConfigureAwait(false);
    }

    public Task<bool> IsCancelledAsync(Guid jobId, CancellationToken cancellationToken) =>
        dbContext.AnalysisJobs
            .AnyAsync(
                job => job.Id == jobId && job.Status == AnalysisJobStatus.Cancelled,
                cancellationToken);

    private Task<AnalysisJob?> FindActiveEquivalentAsync(
        AnalysisJob job,
        CancellationToken cancellationToken) =>
        dbContext.AnalysisJobs
            .AsNoTracking()
            .Where(candidate => candidate.Puuid == job.Puuid
                && candidate.RequestedCount == job.RequestedCount
                && (candidate.Status == AnalysisJobStatus.Queued || candidate.Status == AnalysisJobStatus.Running))
            .OrderBy(candidate => candidate.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
}
