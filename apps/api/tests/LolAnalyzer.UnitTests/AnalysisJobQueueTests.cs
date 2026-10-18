using LolAnalyzer.Application.Jobs;

namespace LolAnalyzer.UnitTests;

public sealed class AnalysisJobQueueTests
{
    [Fact]
    public async Task DispatcherDeduplicatesAJobWhileItIsQueued()
    {
        var dispatcher = new InMemoryAnalysisJobDispatcher(new AnalysisJobExecutionOptions { QueueCapacity = 2 });
        var jobId = Guid.NewGuid();

        await dispatcher.EnqueueAsync(jobId, TestContext.Current.CancellationToken);
        await dispatcher.EnqueueAsync(jobId, TestContext.Current.CancellationToken);

        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        await using var enumerator = dispatcher.ReadAllAsync(cancellation.Token).GetAsyncEnumerator(cancellation.Token);
        Assert.True(await enumerator.MoveNextAsync());
        Assert.Equal(jobId, enumerator.Current);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await enumerator.MoveNextAsync());
    }

    [Fact]
    public async Task DispatcherRejectsInvalidCapacity()
    {
        var options = new AnalysisJobExecutionOptions { QueueCapacity = 0 };

        Assert.Throws<InvalidOperationException>(options.Validate);
        await Task.CompletedTask;
    }
}
