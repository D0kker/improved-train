using LolAnalyzer.Application.Jobs;
using LolAnalyzer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Data;

namespace LolAnalyzer.Infrastructure.Persistence;

public sealed class AnalysisJobRepository(LolAnalyzerDbContext dbContext) : IAnalysisJobRepository
{
    private const string ActiveRequestIndex = "ux_analysis_jobs_active_request";

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
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: ActiveRequestIndex,
        })
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
        await using var transaction = await dbContext.Database
            .BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken)
            .ConfigureAwait(false);
        var queued = AnalysisJobStatus.Queued.ToString();
        var running = AnalysisJobStatus.Running.ToString();
        var job = await dbContext.AnalysisJobs
            .FromSqlInterpolated($"""
                SELECT *
                FROM analysis_jobs
                WHERE status = {queued}
                   OR (status = {running} AND updated_at < {staleBefore})
                ORDER BY CASE WHEN status = {running} THEN 0 ELSE 1 END, created_at, id
                FOR UPDATE SKIP LOCKED
                LIMIT 1
                """)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (job is null)
        {
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return null;
        }

        job.Status = AnalysisJobStatus.Running;
        job.StartedAt ??= now;
        job.UpdatedAt = now;
        job.ErrorCode = null;
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
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
