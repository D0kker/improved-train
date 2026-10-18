namespace LolAnalyzer.Application.Jobs;

public interface IAnalysisJobDispatcher
{
    ValueTask EnqueueAsync(Guid jobId, CancellationToken cancellationToken);

    IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken cancellationToken);
}
