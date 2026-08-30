namespace LolAnalyzer.Domain.Entities;

public sealed class Player
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Puuid { get; set; } = string.Empty;

    public string GameName { get; set; } = string.Empty;

    public string TagLine { get; set; } = string.Empty;

    public string PlatformRegion { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset LastSeenAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<MatchParticipant> MatchParticipants { get; } = new List<MatchParticipant>();
}
