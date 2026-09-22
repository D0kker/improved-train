using LolAnalyzer.Application.Analysis;
using System.Globalization;
using Xunit;

namespace LolAnalyzer.UnitTests;

public sealed class MatchFamiliarityCalculatorTests
{
    private static readonly Guid OwnerId = Id(1);
    private static readonly Guid AllyId = Id(2);
    private static readonly Guid EnemyId = Id(3);
    private static readonly Guid StrangerId = Id(4);
    private static readonly DateTimeOffset BaseTime =
        DateTimeOffset.Parse("2026-01-01T00:00:00Z", CultureInfo.InvariantCulture);

    [Fact]
    public void FirstMatchReportsNoPriorHistoryWithoutTreatingPlayersAsKnown()
    {
        var result = Calculate(Match("MATCH-2", BaseTime, OwnerId, AllyId, EnemyId));

        Assert.Equal(MatchFamiliarityStatus.NoPriorHistory, result.Status);
        Assert.Equal(0, result.KnownPlayers);
        Assert.Equal(2, result.UnknownPlayers);
        Assert.Equal(2, result.EvaluablePlayers);
        Assert.Equal(0, result.FamiliarityPercentage);
    }

    [Fact]
    public void CountsPriorAlliesAndEnemiesButNeverFuturePlayers()
    {
        var result = Calculate(
            Match("MATCH-1", BaseTime, OwnerId, AllyId, EnemyId),
            Match("MATCH-2", BaseTime.AddHours(1), OwnerId, AllyId, EnemyId, StrangerId),
            Match("MATCH-3", BaseTime.AddHours(2), OwnerId, StrangerId));

        Assert.Equal(MatchFamiliarityStatus.Available, result.Status);
        Assert.Equal(2, result.KnownPlayers);
        Assert.Equal(1, result.UnknownPlayers);
        Assert.Equal(3, result.EvaluablePlayers);
        Assert.Equal(66.7m, result.FamiliarityPercentage);
        Assert.Equal([AllyId, EnemyId], result.KnownPlayerIds);
        Assert.Equal(2, result.KnownPlayerDetails.Count);
    }

    [Fact]
    public void UsesRiotMatchIdAsStableTieBreakerForEqualTimestamps()
    {
        var result = MatchFamiliarityCalculator.Calculate(new MatchFamiliarityInput(
            OwnerId,
            "MATCH-B",
            [
            Match("MATCH-A", BaseTime, OwnerId, AllyId),
            Match("MATCH-B", BaseTime, OwnerId, AllyId, EnemyId),
            Match("MATCH-C", BaseTime, OwnerId, EnemyId),
            ]));

        Assert.Equal(1, result.KnownPlayers);
        Assert.Equal([AllyId], result.KnownPlayerIds);
    }

    [Fact]
    public void DeduplicatesAndExcludesOwnerOrUnidentifiableParticipants()
    {
        var target = new FamiliarityMatch(
            "MATCH-2",
            BaseTime.AddHours(1),
            [
                new(OwnerId),
                new(AllyId),
                new(AllyId),
                new(null),
                new(Guid.Empty),
            ]);
        var result = Calculate(Match("MATCH-1", BaseTime, OwnerId, AllyId), target);

        Assert.Equal(1, result.KnownPlayers);
        Assert.Equal(0, result.UnknownPlayers);
        Assert.Equal(1, result.EvaluablePlayers);
        Assert.Equal(100, result.FamiliarityPercentage);
    }

    [Fact]
    public void IgnoresHistoryThatDoesNotContainTheOwner()
    {
        var result = Calculate(
            Match("MATCH-1", BaseTime, AllyId, EnemyId),
            Match("MATCH-2", BaseTime.AddHours(1), OwnerId, AllyId));

        Assert.Equal(MatchFamiliarityStatus.NoPriorHistory, result.Status);
        Assert.Equal(0, result.KnownPlayers);
    }

    [Fact]
    public void ReportsWhenOwnerOrEvaluableParticipantsAreAbsent()
    {
        var ownerMissing = Calculate(Match("MATCH-2", BaseTime, AllyId));
        var nobodyElse = Calculate(Match("MATCH-2", BaseTime, OwnerId, Guid.Empty));

        Assert.Equal(MatchFamiliarityStatus.OwnerNotPresent, ownerMissing.Status);
        Assert.Equal(MatchFamiliarityStatus.NoEvaluableParticipants, nobodyElse.Status);
    }

    [Fact]
    public void ResultDoesNotDependOnInputOrder()
    {
        FamiliarityMatch[] matches =
        [
            Match("MATCH-1", BaseTime, OwnerId, AllyId),
            Match("MATCH-2", BaseTime.AddHours(1), OwnerId, AllyId, EnemyId),
            Match("MATCH-3", BaseTime.AddHours(2), OwnerId, EnemyId),
        ];

        var ordered = Calculate(matches);
        var reversed = Calculate(matches.Reverse().ToArray());

        Assert.Equal(
            ordered with { KnownPlayerIds = [], KnownPlayerDetails = [] },
            reversed with { KnownPlayerIds = [], KnownPlayerDetails = [] });
        Assert.Equal(ordered.KnownPlayerIds, reversed.KnownPlayerIds);
        Assert.Equal(ordered.KnownPlayerDetails, reversed.KnownPlayerDetails);
    }

    [Fact]
    public void KnownPlayerDetailsUseOnlyStrictlyPriorMatches()
    {
        var first = new FamiliarityMatch(
            "MATCH-1",
            BaseTime,
            [
                new(OwnerId, "owner", "Owner", "LAN", 100, true),
                new(AllyId, "ally", "Ally", "LAN", 100, true),
            ]);
        var second = new FamiliarityMatch(
            "MATCH-2",
            BaseTime.AddHours(1),
            [
                new(OwnerId, "owner", "Owner", "LAN", 100, false),
                new(AllyId, "ally", "Ally", "LAN", 200, true),
            ]);
        var target = new FamiliarityMatch(
            "MATCH-3",
            BaseTime.AddHours(2),
            [
                new(OwnerId, "owner", "Owner", "LAN", 100, true),
                new(AllyId, "ally", "Ally", "LAN", 200, false),
                new(StrangerId, "stranger", "Stranger", "LAN", 100, true),
            ]);
        var future = new FamiliarityMatch(
            "MATCH-4",
            BaseTime.AddHours(3),
            [
                new(OwnerId, "owner", "Owner", "LAN", 100, true),
                new(AllyId, "ally", "Ally", "LAN", 100, true),
            ]);

        var result = MatchFamiliarityCalculator.Calculate(
            new MatchFamiliarityInput(OwnerId, "MATCH-3", [future, target, second, first]));

        var known = Assert.Single(result.KnownPlayerDetails);
        Assert.Equal("ally", known.Puuid);
        Assert.Equal("Ally", known.GameName);
        Assert.Equal(2, known.PlayedBefore);
        Assert.Equal(1, known.AllyMatches);
        Assert.Equal(1, known.EnemyMatches);
        Assert.Equal(1, known.WinsTogether);
        Assert.Equal(0, known.LossesTogether);
        Assert.Equal(0, known.WinsAgainst);
        Assert.Equal(1, known.LossesAgainst);
        Assert.Equal(BaseTime, known.FirstEncounterAt);
        Assert.Equal(BaseTime.AddHours(1), known.LastEncounterAt);
    }

    [Fact]
    public void RejectsDuplicateMatchIds()
    {
        var input = new MatchFamiliarityInput(
            OwnerId,
            "MATCH-2",
            [
                Match("MATCH-2", BaseTime, OwnerId),
                Match("MATCH-2", BaseTime.AddHours(1), OwnerId),
            ]);

        Assert.Throws<ArgumentException>(() => MatchFamiliarityCalculator.Calculate(input));
    }

    private static MatchFamiliarityResult Calculate(params FamiliarityMatch[] matches) =>
        MatchFamiliarityCalculator.Calculate(new MatchFamiliarityInput(OwnerId, "MATCH-2", matches));

    private static FamiliarityMatch Match(
        string riotMatchId,
        DateTimeOffset occurredAt,
        params Guid[] playerIds) =>
        new(riotMatchId, occurredAt, playerIds.Select(playerId => new FamiliarityParticipant(playerId)).ToArray());

    private static Guid Id(int value) => Guid.Parse($"00000000-0000-0000-0000-{value:000000000000}");
}
