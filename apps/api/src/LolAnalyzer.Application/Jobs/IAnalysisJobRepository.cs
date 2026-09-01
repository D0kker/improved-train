using LolAnalyzer.Domain.Entities;

namespace LolAnalyzer.Application.Jobs;

public interface IAnalysisJobRepository
{
    Task<AnalysisJob> AddOrGetActiveAsync(AnalysisJob job, CancellationToken cancellationToken);

    Task<AnalysisJob?> FindAsync(Guid jobId, CancellationToken cancellationToken);

    Task<AnalysisJob?> ClaimNextAsync(
        DateTimeOffset now,
        DateTimeOffset staleBefore,
        CancellationToken cancellationToken);

    Task UpdateProgressAsync(
        Guid jobId,
        int matchesProcessed,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task CompleteAsync(Guid jobId, int matchesProcessed, DateTimeOffset now, CancellationToken cancellationToken);

    Task FailAsync(Guid jobId, string errorCode, DateTimeOffset now, CancellationToken cancellationToken);

    Task RequeueAsync(Guid jobId, DateTimeOffset now, CancellationToken cancellationToken);

    Task<AnalysisJob?> CancelAsync(Guid jobId, DateTimeOffset now, CancellationToken cancellationToken);

    Task<bool> IsCancelledAsync(Guid jobId, CancellationToken cancellationToken);
}
