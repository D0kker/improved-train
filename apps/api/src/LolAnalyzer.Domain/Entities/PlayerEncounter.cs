namespace LolAnalyzer.Domain.Entities;

public sealed class PlayerEncounter
{
    public Guid OwnerPlayerId { get; set; }

    public Player OwnerPlayer { get; set; } = null!;

    public Guid OtherPlayerId { get; set; }

    public Player OtherPlayer { get; set; } = null!;

    public int TotalMatches { get; set; }

    public int SameTeamMatches { get; set; }

    public int EnemyTeamMatches { get; set; }

    public int WinsTogether { get; set; }

    public int LossesTogether { get; set; }

    public int WinsAgainst { get; set; }

    public int LossesAgainst { get; set; }

    public DateTimeOffset FirstSeenAt { get; set; }

    public DateTimeOffset LastSeenAt { get; set; }
}
