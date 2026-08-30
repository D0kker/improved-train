using LolAnalyzer.Application.Analysis;
using Xunit;

namespace LolAnalyzer.UnitTests;

public sealed class RelationshipScoreCalculatorTests
{
    private static readonly DateTimeOffset EvaluatedAt = new(2026, 8, 30, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CalculateReturnsLowZeroScoreWhenThereIsNoEvidence()
    {
        var calculator = new RelationshipScoreCalculator(new RelationshipScoreOptions());

        var result = calculator.Calculate(new RelationshipScoreInput(EvaluatedAt, []));

        Assert.Equal(0, result.Score);
        Assert.Equal(RelationshipConfidence.Low, result.Confidence);
        Assert.Equal("LOW", result.ConfidenceLabel);
        Assert.Equal(0m, result.Factors.MatchesTogether.WeightedScore);
        Assert.Equal(0m, result.Factors.RecentFrequency.WeightedScore);
        Assert.Equal(0m, result.Factors.ConsecutiveMatches.WeightedScore);
        Assert.Equal(0m, result.Factors.SameTeam.WeightedScore);
    }

    [Fact]
    public void CalculateIsDeterministicRegardlessOfEvidenceOrder()
    {
        var calculator = new RelationshipScoreCalculator(new RelationshipScoreOptions
        {
            RecentMatchWindow = 4,
            RecencyWindow = TimeSpan.FromDays(30),
        });
        RelationshipMatchEvidence[] evidence =
        [
            Match("TEST_1", daysAgo: 4, encountered: true, sameTeam: true),
            Match("TEST_2", daysAgo: 3, encountered: true, sameTeam: true),
            Match("TEST_3", daysAgo: 2, encountered: false, sameTeam: false),
            Match("TEST_4", daysAgo: 1, encountered: true, sameTeam: false),
        ];

        var chronological = calculator.Calculate(new RelationshipScoreInput(EvaluatedAt, evidence));
        var shuffled = calculator.Calculate(new RelationshipScoreInput(EvaluatedAt, [evidence[2], evidence[0], evidence[3], evidence[1]]));

        Assert.Equal(chronological, shuffled);
        Assert.InRange(chronological.Score, 0, 100);
        Assert.Equal(3, chronological.Factors.MatchesTogether.EvidenceCount);
        Assert.Equal(4, chronological.Factors.RecentFrequency.EvidenceTotal);
        Assert.Equal(2, chronological.Factors.ConsecutiveMatches.EvidenceCount);
        Assert.Equal(2, chronological.Factors.SameTeam.EvidenceCount);
    }

    [Fact]
    public void CalculateCapsEveryFactorAndTheFinalScoreAtOneHundred()
    {
        var calculator = new RelationshipScoreCalculator(new RelationshipScoreOptions
        {
            MatchesTogetherForFullScore = 1,
            ConsecutiveMatchesForFullScore = 1,
        });
        var evidence = Enumerable.Range(1, 30)
            .Select(index => Match($"TEST_{index}", index, encountered: true, sameTeam: true))
            .ToArray();

        var result = calculator.Calculate(new RelationshipScoreInput(EvaluatedAt, evidence));

        Assert.Equal(100, result.Score);
        Assert.Equal(RelationshipConfidence.VeryHigh, result.Confidence);
        Assert.All(
            new[]
            {
                result.Factors.MatchesTogether,
                result.Factors.RecentFrequency,
                result.Factors.ConsecutiveMatches,
                result.Factors.SameTeam,
            },
            factor => Assert.InRange(factor.NormalizedValue, 0m, 1m));
    }

    [Fact]
    public void ConstructorRejectsInvalidConfiguration()
    {
        var invalidWeights = new RelationshipScoreOptions { MatchesTogetherWeight = -1, SameTeamWeight = 51 };
        var invalidWindow = new RelationshipScoreOptions { RecentMatchWindow = 0 };
        var invalidThresholds = new RelationshipScoreOptions { HighThreshold = 25 };

        Assert.Throws<ArgumentOutOfRangeException>(() => new RelationshipScoreCalculator(invalidWeights));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RelationshipScoreCalculator(invalidWindow));
        Assert.Throws<ArgumentException>(() => new RelationshipScoreCalculator(invalidThresholds));
    }

    [Theory]
    [InlineData(24, RelationshipConfidence.Low)]
    [InlineData(25, RelationshipConfidence.Medium)]
    [InlineData(49, RelationshipConfidence.Medium)]
    [InlineData(50, RelationshipConfidence.High)]
    [InlineData(74, RelationshipConfidence.High)]
    [InlineData(75, RelationshipConfidence.VeryHigh)]
    public void ClassifyUsesTheConfiguredInclusiveBoundaries(int score, RelationshipConfidence expected)
    {
        var calculator = new RelationshipScoreCalculator(new RelationshipScoreOptions());

        var confidence = calculator.Classify(score);

        Assert.Equal(expected, confidence);
    }

    private static RelationshipMatchEvidence Match(
        string matchId,
        int daysAgo,
        bool encountered,
        bool sameTeam) =>
        new(matchId, EvaluatedAt.AddDays(-daysAgo), encountered, sameTeam);
}
