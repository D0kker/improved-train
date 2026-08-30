namespace LolAnalyzer.Application.Analysis;

public enum RelationshipConfidence
{
    Low,
    Medium,
    High,
    VeryHigh,
}

public static class RelationshipConfidenceExtensions
{
    public static string ToLabel(this RelationshipConfidence confidence) => confidence switch
    {
        RelationshipConfidence.Low => "LOW",
        RelationshipConfidence.Medium => "MEDIUM",
        RelationshipConfidence.High => "HIGH",
        RelationshipConfidence.VeryHigh => "VERY_HIGH",
        _ => throw new ArgumentOutOfRangeException(nameof(confidence), confidence, "Unknown confidence level."),
    };
}

public sealed record RelationshipMatchEvidence(
    string MatchId,
    DateTimeOffset OccurredAt,
    bool Encountered,
    bool SameTeam);

public sealed record RelationshipScoreInput(
    DateTimeOffset EvaluatedAt,
    IReadOnlyList<RelationshipMatchEvidence> Matches);

public sealed record RelationshipScoreFactor(
    int EvidenceCount,
    int EvidenceTotal,
    int Weight,
    decimal NormalizedValue,
    decimal WeightedScore);

public sealed record RelationshipScoreFactors(
    RelationshipScoreFactor MatchesTogether,
    RelationshipScoreFactor RecentFrequency,
    RelationshipScoreFactor ConsecutiveMatches,
    RelationshipScoreFactor SameTeam);

public sealed record RelationshipScoreResult(
    int Score,
    RelationshipConfidence Confidence,
    RelationshipScoreFactors Factors)
{
    public string ConfidenceLabel => Confidence.ToLabel();
}
