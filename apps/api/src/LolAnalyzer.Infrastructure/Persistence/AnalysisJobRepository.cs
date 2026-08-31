using LolAnalyzer.Application.Jobs;
using LolAnalyzer.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LolAnalyzer.Infrastructure.Persistence;

public sealed class AnalysisJobRepository(LolAnalyzerDbContext dbContext) : IAnalysisJobRepository
{
    public async Task AddAsync(AnalysisJob job, CancellationToken cancellationToken)
    {
        dbContext.AnalysisJobs.Add(job);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<AnalysisJob?> FindAsync(Guid jobId, CancellationToken cancellationToken) =>
        dbContext.AnalysisJobs
            .AsNoTracking()
            .SingleOrDefaultAsync(job => job.Id == jobId, cancellationToken);
}
