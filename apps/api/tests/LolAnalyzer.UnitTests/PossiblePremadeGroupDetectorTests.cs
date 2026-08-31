using LolAnalyzer.Application.Analysis;
using Xunit;

namespace LolAnalyzer.UnitTests;

public sealed class PossiblePremadeGroupDetectorTests
{
    private static readonly Guid A = Id(1);
    private static readonly Guid B = Id(2);
    private static readonly Guid C = Id(3);
    private static readonly Guid D = Id(4);
    private static readonly Guid E = Id(5);

    [Fact]
    public void DetectsACompleteCliqueAndRejectsAnIncompleteOne()
    {
        var complete = Detector().Detect([Pair(A, B), Pair(A, C), Pair(B, C)]);
        var incomplete = Detector().Detect([Pair(A, B), Pair(A, C), Pair(B, C, PremadeClassification.NoEvidence)]);

        Assert.Equal([A, B, C], Assert.Single(complete).PlayerIds);
        Assert.Empty(incomplete);
    }

    [Fact]
    public void ReturnsOnlyTheMaximalCanonicalGroup()
    {
        var groups = Detector().Detect(CompleteGraph([A, B, C, D]).ToArray());

        var group = Assert.Single(groups);
        Assert.Equal([A, B, C, D], group.PlayerIds);
        Assert.Equal("possible premade group", group.Label);
    }

    [Fact]
    public void AllowsDistinctOverlappingMaximalGroups()
    {
        PremadeGroupPair[] pairs =
        [
            Pair(A, B), Pair(A, C), Pair(B, C),
            Pair(B, D), Pair(C, D),
        ];

        var groups = Detector().Detect(pairs);

        Assert.Equal(2, groups.Count);
        Assert.Contains(groups, group => group.PlayerIds.SequenceEqual([A, B, C]));
        Assert.Contains(groups, group => group.PlayerIds.SequenceEqual([B, C, D]));
    }

    [Fact]
    public void SupportsFivePlayersAndUsesTheWeakestPairClassification()
    {
        var pairs = CompleteGraph([A, B, C, D, E]).ToArray();
        pairs[0] = pairs[0] with { Classification = PremadeClassification.LikelyPremade };

        var group = Assert.Single(Detector().Detect(pairs));

        Assert.Equal(5, group.PlayerIds.Count);
        Assert.Equal(PremadeClassification.PossiblePremade, group.Classification);
    }

    [Fact]
    public void EnforcesCandidateAndCombinationLimits()
    {
        var candidateLimited = Detector(new PremadeGroupDetectionOptions
        {
            MaximumCandidates = 3,
            MaximumGroupSize = 3,
        });
        var workLimited = Detector(new PremadeGroupDetectionOptions
        {
            MaximumCandidates = 5,
            MaximumCombinations = 1,
        });

        Assert.Throws<ArgumentException>(() => candidateLimited.Detect([Pair(A, B), Pair(C, D)]));
        Assert.Throws<ArgumentException>(() => workLimited.Detect(CompleteGraph([A, B, C, D]).ToArray()));
    }

    private static PossiblePremadeGroupDetector Detector(PremadeGroupDetectionOptions? options = null) =>
        new(options ?? new PremadeGroupDetectionOptions());

    private static IEnumerable<PremadeGroupPair> CompleteGraph(Guid[] players)
    {
        for (var first = 0; first < players.Length; first++)
        {
            for (var second = first + 1; second < players.Length; second++)
            {
                yield return Pair(players[first], players[second]);
            }
        }
    }

    private static PremadeGroupPair Pair(
        Guid first,
        Guid second,
        PremadeClassification classification = PremadeClassification.PossiblePremade) =>
        new(first, second, classification);

    private static Guid Id(int value) => Guid.Parse($"00000000-0000-0000-0000-{value:000000000000}");
}
