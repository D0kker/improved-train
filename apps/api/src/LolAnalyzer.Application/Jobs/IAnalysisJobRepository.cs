using LolAnalyzer.Domain.Entities;

namespace LolAnalyzer.Application.Jobs;

public interface IAnalysisJobRepository
{
    Task AddAsync(AnalysisJob job, CancellationToken cancellationToken);

    Task<AnalysisJob?> FindAsync(Guid jobId, CancellationToken cancellationToken);
}
