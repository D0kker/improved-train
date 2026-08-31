namespace LolAnalyzer.Domain.Entities;

public enum AnalysisJobStatus
{
    Queued,
    Running,
    Completed,
    Failed,
    Cancelled,
}

public sealed class AnalysisJob
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Puuid { get; set; } = string.Empty;

    public int RequestedCount { get; set; }

    public AnalysisJobStatus Status { get; set; } = AnalysisJobStatus.Queued;

    public int MatchesProcessed { get; set; }

    public string? ErrorCode { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }
}
