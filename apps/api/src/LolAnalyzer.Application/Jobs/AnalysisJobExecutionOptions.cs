namespace LolAnalyzer.Application.Jobs;

public sealed class AnalysisJobExecutionOptions
{
    public const string SectionName = "AnalysisJobs";

    public int BatchSize { get; init; } = 20;

    public TimeSpan PollInterval { get; init; } = TimeSpan.FromSeconds(2);

    public TimeSpan LeaseTimeout { get; init; } = TimeSpan.FromMinutes(15);

    public void Validate()
    {
        if (BatchSize is < 1 or > 100)
        {
            throw new InvalidOperationException("Analysis job batch size must be between 1 and 100.");
        }

        if (PollInterval < TimeSpan.FromMilliseconds(100) || PollInterval > TimeSpan.FromMinutes(1))
        {
            throw new InvalidOperationException("Analysis job poll interval must be between 100 ms and 1 minute.");
        }

        if (LeaseTimeout < TimeSpan.FromMinutes(1) || LeaseTimeout > TimeSpan.FromHours(24))
        {
            throw new InvalidOperationException("Analysis job lease timeout must be between 1 minute and 24 hours.");
        }
    }
}
