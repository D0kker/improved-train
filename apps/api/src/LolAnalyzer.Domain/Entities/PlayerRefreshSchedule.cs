namespace LolAnalyzer.Domain.Entities;

public sealed class PlayerRefreshSchedule
{
    public string Puuid { get; set; } = string.Empty;

    public int RequestedCount { get; set; }

    public int IntervalMinutes { get; set; }

    public bool Enabled { get; set; }

    public DateTimeOffset NextRunAt { get; set; }

    public DateTimeOffset? LastEnqueuedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
