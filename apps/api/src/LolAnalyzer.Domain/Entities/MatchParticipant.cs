namespace LolAnalyzer.Domain.Entities;

public sealed class MatchParticipant
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid MatchId { get; set; }

    public Match Match { get; set; } = null!;

    public Guid PlayerId { get; set; }

    public Player Player { get; set; } = null!;

    public int TeamId { get; set; }

    public int ParticipantId { get; set; }

    public int ChampionId { get; set; }

    public string ChampionName { get; set; } = string.Empty;

    public string? TeamPosition { get; set; }

    public string? IndividualPosition { get; set; }

    public int Kills { get; set; }

    public int Deaths { get; set; }

    public int Assists { get; set; }

    public bool Win { get; set; }

    public int GoldEarned { get; set; }

    public int TotalDamageDealtToChampions { get; set; }

    public int VisionScore { get; set; }

    public int Cs { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
