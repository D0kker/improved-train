namespace LolAnalyzer.Application.Analysis;

public sealed class RelationshipScoreOptions
{
    public const string SectionName = "RelationshipScore";

    public int MatchesTogetherWeight { get; init; } = 30;

    public int RecentFrequencyWeight { get; init; } = 30;

    public int ConsecutiveMatchesWeight { get; init; } = 20;

    public int SameTeamWeight { get; init; } = 20;

    public int MatchesTogetherForFullScore { get; init; } = 10;

    public int ConsecutiveMatchesForFullScore { get; init; } = 5;

    public int RecentMatchWindow { get; init; } = 20;

    public TimeSpan RecencyWindow { get; init; } = TimeSpan.FromDays(90);

    public int MediumThreshold { get; init; } = 25;

    public int HighThreshold { get; init; } = 50;

    public int VeryHighThreshold { get; init; } = 75;

    public void Validate()
    {
        ValidateNonNegativeWeight(MatchesTogetherWeight, nameof(MatchesTogetherWeight));
        ValidateNonNegativeWeight(RecentFrequencyWeight, nameof(RecentFrequencyWeight));
        ValidateNonNegativeWeight(ConsecutiveMatchesWeight, nameof(ConsecutiveMatchesWeight));
        ValidateNonNegativeWeight(SameTeamWeight, nameof(SameTeamWeight));

        var totalWeight = MatchesTogetherWeight
            + RecentFrequencyWeight
            + ConsecutiveMatchesWeight
            + SameTeamWeight;
        if (totalWeight != 100)
        {
            throw new ArgumentException("Relationship score weights must add up to 100.");
        }

        ValidatePositive(MatchesTogetherForFullScore, nameof(MatchesTogetherForFullScore));
        ValidatePositive(ConsecutiveMatchesForFullScore, nameof(ConsecutiveMatchesForFullScore));
        ValidatePositive(RecentMatchWindow, nameof(RecentMatchWindow));

        if (RecencyWindow <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(RecencyWindow),
                RecencyWindow,
                "The recency window must be greater than zero.");
        }

        if (MediumThreshold is < 1 or > 98
            || HighThreshold <= MediumThreshold
            || VeryHighThreshold <= HighThreshold
            || VeryHighThreshold > 100)
        {
            throw new ArgumentException(
                "Confidence thresholds must be strictly increasing within the score range: "
                + "0 < medium < high < very high <= 100.");
        }
    }

    private static void ValidateNonNegativeWeight(int value, string name)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(name, value, "Relationship score weights cannot be negative.");
        }
    }

    private static void ValidatePositive(int value, string name)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(name, value, "The value must be greater than zero.");
        }
    }
}
