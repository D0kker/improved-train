namespace LolAnalyzer.Application.Analysis;

public enum PremadeClassification
{
    NoEvidence,
    PossiblePremade,
    LikelyPremade,
}

public static class PremadeClassificationExtensions
{
    public static string? ToLabel(this PremadeClassification classification) => classification switch
    {
        PremadeClassification.NoEvidence => null,
        PremadeClassification.PossiblePremade => "possible premade",
        PremadeClassification.LikelyPremade => "likely premade",
        _ => throw new ArgumentOutOfRangeException(nameof(classification), classification, "Unknown classification."),
    };
}

public sealed record PremadeDetectionInput(
    int MatchesTogether,
    int SameTeamMatches,
    RelationshipConfidence RelationshipConfidence);

public sealed record PremadeDetectionResult(
    PremadeClassification Classification,
    int MatchesTogether,
    int SameTeamMatches,
    decimal SameTeamRatio,
    RelationshipConfidence RelationshipConfidence)
{
    public string? Label => Classification.ToLabel();

    public bool IsDetected => Classification is not PremadeClassification.NoEvidence;
}
