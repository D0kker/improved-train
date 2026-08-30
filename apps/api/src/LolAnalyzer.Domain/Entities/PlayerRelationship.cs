namespace LolAnalyzer.Domain.Entities;

public sealed class PlayerRelationship
{
    public Guid PlayerAId { get; set; }

    public Player PlayerA { get; set; } = null!;

    public Guid PlayerBId { get; set; }

    public Player PlayerB { get; set; } = null!;

    public int MatchesTogether { get; set; }

    public int SameTeamMatches { get; set; }

    public int OppositeTeamMatches { get; set; }

    public int RecentMatchesTogether { get; set; }

    public int ConsecutiveMatches { get; set; }

    public DateTimeOffset FirstSeenAt { get; set; }

    public DateTimeOffset LastSeenAt { get; set; }

    public int RelationshipScore { get; set; }

    public string RelationshipConfidence { get; set; } = "LOW";
}
