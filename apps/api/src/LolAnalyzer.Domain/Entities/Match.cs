using System.Text.Json;

namespace LolAnalyzer.Domain.Entities;

public sealed class Match
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string RiotMatchId { get; set; } = string.Empty;

    public int? QueueId { get; set; }

    public DateTimeOffset? GameCreation { get; set; }

    public DateTimeOffset? GameStartTimestamp { get; set; }

    public DateTimeOffset? GameEndTimestamp { get; set; }

    public int? GameDurationSeconds { get; set; }

    public string? GameVersion { get; set; }

    public JsonDocument? RawData { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<MatchParticipant> Participants { get; } = new List<MatchParticipant>();
}
