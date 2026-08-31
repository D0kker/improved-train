using LolAnalyzer.Application.Analysis;
using Xunit;

namespace LolAnalyzer.UnitTests;

public sealed class PossiblePremadeDetectorTests
{
    [Theory]
    [InlineData(3, 2, RelationshipConfidence.Medium, PremadeClassification.PossiblePremade)]
    [InlineData(5, 4, RelationshipConfidence.High, PremadeClassification.LikelyPremade)]
    public void DetectIncludesExactConfigurableBoundaries(
        int matchesTogether,
        int sameTeamMatches,
        RelationshipConfidence confidence,
        PremadeClassification expected)
    {
        var result = CreateDetector().Detect(new PremadeDetectionInput(
            matchesTogether,
            sameTeamMatches,
            confidence));

        Assert.Equal(expected, result.Classification);
        Assert.True(result.IsDetected);
        Assert.NotNull(result.Label);
    }

    [Theory]
    [InlineData(2, 2, RelationshipConfidence.VeryHigh)]
    [InlineData(10, 2, RelationshipConfidence.VeryHigh)]
    [InlineData(10, 10, RelationshipConfidence.Low)]
    public void DetectRejectsInsufficientCasualOrMostlyOpponentEvidence(
        int matchesTogether,
        int sameTeamMatches,
        RelationshipConfidence confidence)
    {
        var result = CreateDetector().Detect(new PremadeDetectionInput(
            matchesTogether,
            sameTeamMatches,
            confidence));

        Assert.Equal(PremadeClassification.NoEvidence, result.Classification);
        Assert.False(result.IsDetected);
        Assert.Null(result.Label);
    }

    [Fact]
    public void DetectUsesInjectedThresholds()
    {
        var detector = new PossiblePremadeDetector(new PremadeDetectionOptions
        {
            PossibleMinimumMatchesTogether = 2,
            PossibleMinimumSameTeamRatio = 0.5m,
            PossibleMinimumConfidence = RelationshipConfidence.Low,
            LikelyMinimumMatchesTogether = 4,
            LikelyMinimumSameTeamRatio = 0.75m,
            LikelyMinimumConfidence = RelationshipConfidence.Medium,
        });

        var result = detector.Detect(new PremadeDetectionInput(2, 1, RelationshipConfidence.Low));

        Assert.Equal(PremadeClassification.PossiblePremade, result.Classification);
    }

    [Fact]
    public void OptionsRejectLikelyThresholdsWeakerThanPossibleThresholds()
    {
        var options = new PremadeDetectionOptions
        {
            PossibleMinimumMatchesTogether = 5,
            LikelyMinimumMatchesTogether = 4,
        };

        Assert.Throws<ArgumentException>(options.Validate);
    }

    private static PossiblePremadeDetector CreateDetector() => new(new PremadeDetectionOptions());
}
