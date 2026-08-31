namespace LolAnalyzer.Application.Analysis;

public sealed class PremadeDetectionOptions
{
    public const string SectionName = "PremadeDetection";

    public int PossibleMinimumMatchesTogether { get; init; } = 3;

    public decimal PossibleMinimumSameTeamRatio { get; init; } = 0.6m;

    public RelationshipConfidence PossibleMinimumConfidence { get; init; } = RelationshipConfidence.Medium;

    public int LikelyMinimumMatchesTogether { get; init; } = 5;

    public decimal LikelyMinimumSameTeamRatio { get; init; } = 0.8m;

    public RelationshipConfidence LikelyMinimumConfidence { get; init; } = RelationshipConfidence.High;

    public void Validate()
    {
        ValidatePositive(PossibleMinimumMatchesTogether, nameof(PossibleMinimumMatchesTogether));
        ValidatePositive(LikelyMinimumMatchesTogether, nameof(LikelyMinimumMatchesTogether));
        ValidateRatio(PossibleMinimumSameTeamRatio, nameof(PossibleMinimumSameTeamRatio));
        ValidateRatio(LikelyMinimumSameTeamRatio, nameof(LikelyMinimumSameTeamRatio));
        ValidateConfidence(PossibleMinimumConfidence, nameof(PossibleMinimumConfidence));
        ValidateConfidence(LikelyMinimumConfidence, nameof(LikelyMinimumConfidence));

        if (LikelyMinimumMatchesTogether < PossibleMinimumMatchesTogether
            || LikelyMinimumSameTeamRatio < PossibleMinimumSameTeamRatio
            || LikelyMinimumConfidence < PossibleMinimumConfidence)
        {
            throw new ArgumentException(
                "Likely premade thresholds cannot be weaker than possible premade thresholds.");
        }
    }

    private static void ValidatePositive(int value, string name)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(name, value, "The minimum must be greater than zero.");
        }
    }

    private static void ValidateRatio(decimal value, string name)
    {
        if (value is < 0m or > 1m)
        {
            throw new ArgumentOutOfRangeException(name, value, "The same-team ratio must be between zero and one.");
        }
    }

    private static void ValidateConfidence(RelationshipConfidence value, string name)
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(name, value, "The confidence level is not defined.");
        }
    }
}
