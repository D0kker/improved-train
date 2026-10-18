using System.Collections.Concurrent;
using System.Threading.Channels;

namespace LolAnalyzer.Application.Jobs;

public sealed class InMemoryAnalysisJobDispatcher : IAnalysisJobDispatcher
{
    private readonly Channel<Guid> channel;
    private readonly ConcurrentDictionary<Guid, byte> queued = new();

    public InMemoryAnalysisJobDispatcher(AnalysisJobExecutionOptions options)
    {
        channel = Channel.CreateBounded<Guid>(new BoundedChannelOptions(options.QueueCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false,
        });
    }

    public async ValueTask EnqueueAsync(Guid jobId, CancellationToken cancellationToken)
    {
        if (!queued.TryAdd(jobId, 0))
        {
            return;
        }

        try
        {
            await channel.Writer.WriteAsync(jobId, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            queued.TryRemove(jobId, out _);
            throw;
        }
    }

    public async IAsyncEnumerable<Guid> ReadAllAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var jobId in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            queued.TryRemove(jobId, out _);
            yield return jobId;
        }
    }
}
