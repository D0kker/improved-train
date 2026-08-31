using System.Globalization;
using LolAnalyzer.Application.Analysis;
using Xunit;

namespace LolAnalyzer.UnitTests;

public sealed class PlayerRelationshipAnalyzerTests
{
    private static readonly Guid PlayerA = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid PlayerB = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private static readonly Guid PlayerC = Guid.Parse("00000000-0000-0000-0000-000000000003");

    [Fact]
    public void AnalyzeBuildsOneCanonicalPairWithStableChronologyAndTeamCounters()
    {
        var first = DateTimeOffset.Parse("2026-01-01T00:00:00Z", CultureInfo.InvariantCulture);
        var analyzer = CreateAnalyzer();

        var relationships = analyzer.Analyze(
        [
            Match("MATCH_3", first.AddDays(2), (PlayerB, 200), (PlayerA, 100)),
            Match("MATCH_2", first.AddDays(1), (PlayerA, 100), (PlayerC, 100)),
            Match("MATCH_1", first, (PlayerB, 100), (PlayerA, 100)),
        ]);

        var relationship = Assert.Single(
            relationships,
            candidate => candidate.PlayerAId == PlayerA && candidate.PlayerBId == PlayerB);
        Assert.Equal(2, relationship.MatchesTogether);
        Assert.Equal(1, relationship.SameTeamMatches);
        Assert.Equal(1, relationship.OppositeTeamMatches);
        Assert.Equal(1, relationship.ConsecutiveMatches);
        Assert.Equal(first, relationship.FirstSeenAt);
        Assert.Equal(first.AddDays(2), relationship.LastSeenAt);
        Assert.InRange(relationship.RelationshipScore, 0, 100);
    }

    [Fact]
    public void AnalyzeIsDeterministicWithoutSelfOrDuplicatePairs()
    {
        var analyzer = CreateAnalyzer();
        var input = new[]
        {
            Match("MATCH_1", DateTimeOffset.UnixEpoch, (PlayerB, 100), (PlayerA, 100), (PlayerC, 200)),
            Match("MATCH_2", DateTimeOffset.UnixEpoch.AddMinutes(20), (PlayerC, 100), (PlayerB, 100)),
        };

        var firstRun = analyzer.Analyze(input);
        var secondRun = analyzer.Analyze(input.Reverse().ToArray());

        Assert.Equal(firstRun, secondRun);
        Assert.Equal(3, firstRun.Count);
        Assert.All(firstRun, relationship => Assert.True(relationship.PlayerAId.CompareTo(relationship.PlayerBId) < 0));
        Assert.Equal(firstRun.Count, firstRun.Select(item => (item.PlayerAId, item.PlayerBId)).Distinct().Count());
    }

    [Fact]
    public async Task RebuildAsyncReplacesTheGlobalSnapshotOnEveryRun()
    {
        var matches = new[]
        {
            Match("MATCH_1", DateTimeOffset.UnixEpoch, (PlayerA, 100), (PlayerB, 100)),
        };
        var repository = new RecordingRelationshipRepository(matches);
        var service = new PlayerRelationshipAnalysisService(
            repository,
            CreateAnalyzer(),
            new PlayerRelationshipAnalysisOptions { ReadBatchSize = 1, WriteBatchSize = 1 });

        var first = await service.RebuildAsync(TestContext.Current.CancellationToken);
        var second = await service.RebuildAsync(TestContext.Current.CancellationToken);

        Assert.Equal(first, second);
        Assert.Equal(2, repository.ReplacementCount);
        Assert.Equal(repository.Snapshots[0], repository.Snapshots[1]);
        Assert.All(repository.ReadBatchSizes, size => Assert.Equal(1, size));
        Assert.All(repository.WriteBatchSizes, size => Assert.Equal(1, size));
    }

    private static PlayerRelationshipAnalyzer CreateAnalyzer() =>
        new(new RelationshipScoreCalculator(new RelationshipScoreOptions()));

    private static RelationshipMatchSnapshot Match(
        string matchId,
        DateTimeOffset occurredAt,
        params (Guid PlayerId, int TeamId)[] participants) =>
        new(
            matchId,
            occurredAt,
            participants.Select(item => new RelationshipParticipant(item.PlayerId, item.TeamId)).ToArray());

    private sealed class RecordingRelationshipRepository(
        IReadOnlyList<RelationshipMatchSnapshot> matches) : IPlayerRelationshipRepository
    {
        public int ReplacementCount { get; private set; }

        public List<int> ReadBatchSizes { get; } = [];

        public List<int> WriteBatchSizes { get; } = [];

        public List<IReadOnlyCollection<PlayerRelationshipAggregate>> Snapshots { get; } = [];

        public Task<IReadOnlyList<RelationshipMatchSnapshot>> LoadMatchesAsync(
            int batchSize,
            CancellationToken cancellationToken)
        {
            ReadBatchSizes.Add(batchSize);
            return Task.FromResult(matches);
        }

        public Task ReplaceRelationshipsAsync(
            IReadOnlyCollection<PlayerRelationshipAggregate> relationships,
            int batchSize,
            CancellationToken cancellationToken)
        {
            ReplacementCount++;
            WriteBatchSizes.Add(batchSize);
            Snapshots.Add(relationships.ToArray());
            return Task.CompletedTask;
        }

        public Task<PagedPlayerRelationshipQuery?> GetRelationshipsAsync(
            string puuid,
            int page,
            int pageSize,
            RelationshipConfidence minimumConfidence,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
