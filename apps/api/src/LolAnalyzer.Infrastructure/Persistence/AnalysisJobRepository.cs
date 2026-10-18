using LolAnalyzer.Application.Jobs;
using LolAnalyzer.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LolAnalyzer.Infrastructure.Persistence;

public sealed class AnalysisJobRepository(LolAnalyzerDbContext dbContext) : IAnalysisJobRepository
{
    private static readonly SemaphoreSlim StateGate = new(1, 1);

    public async Task<AnalysisJob> AddOrGetActiveAsync(AnalysisJob job, CancellationToken cancellationToken)
    {
        await StateGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var activeJob = await FindActiveEquivalentAsync(job, cancellationToken).ConfigureAwait(false);
            if (activeJob is not null)
            {
                return activeJob;
            }

            dbContext.AnalysisJobs.Add(job);
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
        finally
        {
            StateGate.Release();
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
        await StateGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
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
        finally
        {
            StateGate.Release();
        }
    }

    public async Task UpdateProgressAsync(
        Guid jobId,
        int matchesProcessed,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await MutateAsync(jobId, AnalysisJobStatus.Running, job =>
        {
            job.MatchesProcessed = matchesProcessed;
            job.UpdatedAt = now;
        }, cancellationToken).ConfigureAwait(false);
    }

    public async Task CompleteAsync(
        Guid jobId,
        int matchesProcessed,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await MutateAsync(jobId, AnalysisJobStatus.Running, job =>
        {
            job.Status = AnalysisJobStatus.Completed;
            job.MatchesProcessed = matchesProcessed;
            job.ErrorCode = null;
            job.UpdatedAt = now;
            job.CompletedAt = now;
        }, cancellationToken).ConfigureAwait(false);
    }

    public async Task FailAsync(
        Guid jobId,
        string errorCode,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await MutateAsync(jobId, AnalysisJobStatus.Running, job =>
        {
            job.Status = AnalysisJobStatus.Failed;
            job.ErrorCode = errorCode;
            job.UpdatedAt = now;
            job.CompletedAt = now;
        }, cancellationToken).ConfigureAwait(false);
    }

    public async Task RequeueAsync(
        Guid jobId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await MutateAsync(jobId, AnalysisJobStatus.Running, job =>
        {
            job.Status = AnalysisJobStatus.Queued;
            job.UpdatedAt = now;
        }, cancellationToken).ConfigureAwait(false);
    }

    public async Task<AnalysisJob?> CancelAsync(
        Guid jobId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await StateGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var job = await dbContext.AnalysisJobs.SingleOrDefaultAsync(candidate => candidate.Id == jobId, cancellationToken).ConfigureAwait(false);
            if (job is not null && job.Status is AnalysisJobStatus.Queued or AnalysisJobStatus.Running)
            {
                job.Status = AnalysisJobStatus.Cancelled;
                job.ErrorCode = null;
                job.UpdatedAt = now;
                job.CompletedAt = now;
                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            return job;
        }
        finally
        {
            StateGate.Release();
        }
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

    private async Task MutateAsync(
        Guid jobId,
        AnalysisJobStatus expectedStatus,
        Action<AnalysisJob> mutation,
        CancellationToken cancellationToken)
    {
        await StateGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var job = await dbContext.AnalysisJobs.SingleOrDefaultAsync(candidate => candidate.Id == jobId, cancellationToken).ConfigureAwait(false);
            if (job is null || job.Status != expectedStatus)
            {
                return;
            }

            mutation(job);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            StateGate.Release();
        }
    }
}
