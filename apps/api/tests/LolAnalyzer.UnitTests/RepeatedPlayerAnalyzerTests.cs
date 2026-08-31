using LolAnalyzer.Application.Analysis;
using System.Globalization;
using Xunit;

namespace LolAnalyzer.UnitTests;

public sealed class RepeatedPlayerAnalyzerTests
{
    private static readonly Guid OwnerId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid AllyId = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private static readonly Guid EnemyId = Guid.Parse("00000000-0000-0000-0000-000000000003");

    [Fact]
    public void AnalyzeBuildsDirectedAllyAndEnemyCountersWithEncounterBounds()
    {
        var first = DateTimeOffset.Parse("2026-01-01T00:00:00Z", CultureInfo.InvariantCulture);
        var last = first.AddDays(2);
        var input = new PlayerAnalysisInput(
            OwnerId,
            [
                Match(first, ownerWon: true, (AllyId, 100), (EnemyId, 200)),
                Match(last, ownerWon: false, (AllyId, 100), (EnemyId, 200)),
            ]);

        var result = RepeatedPlayerAnalyzer.Analyze(input);

        var ally = Assert.Single(result, encounter => encounter.OtherPlayerId == AllyId);
        Assert.Equal(2, ally.TotalMatches);
        Assert.Equal(2, ally.SameTeamMatches);
        Assert.Equal(1, ally.WinsTogether);
        Assert.Equal(1, ally.LossesTogether);
        Assert.Equal(first, ally.FirstSeenAt);
        Assert.Equal(last, ally.LastSeenAt);

        var enemy = Assert.Single(result, encounter => encounter.OtherPlayerId == EnemyId);
        Assert.Equal(2, enemy.EnemyTeamMatches);
        Assert.Equal(1, enemy.WinsAgainst);
        Assert.Equal(1, enemy.LossesAgainst);
    }

    [Fact]
    public void AnalyzeIsDeterministicAndNeverCreatesASelfEncounter()
    {
        var match = Match(
            DateTimeOffset.UnixEpoch,
            ownerWon: true,
            (OwnerId, 100),
            (AllyId, 100),
            (AllyId, 100));
        var firstRun = RepeatedPlayerAnalyzer.Analyze(new PlayerAnalysisInput(OwnerId, [match]));
        var secondRun = RepeatedPlayerAnalyzer.Analyze(new PlayerAnalysisInput(OwnerId, [match]));

        Assert.Equal(firstRun, secondRun);
        Assert.Equal(AllyId, Assert.Single(firstRun).OtherPlayerId);
        Assert.DoesNotContain(firstRun, encounter => encounter.OtherPlayerId == OwnerId);
    }

    [Fact]
    public async Task RebuildAsyncReplacesTheOwnersSnapshotOnEveryRun()
    {
        var repository = new RecordingAnalysisRepository(new PlayerAnalysisInput(
            OwnerId,
            [Match(DateTimeOffset.UnixEpoch, ownerWon: true, (AllyId, 100))]));
        var service = new RepeatedPlayerAnalysisService(repository);

        await service.RebuildAsync("owner-puuid", TestContext.Current.CancellationToken);
        await service.RebuildAsync("owner-puuid", TestContext.Current.CancellationToken);

        Assert.Equal(2, repository.ReplacementCount);
        Assert.All(repository.Snapshots, snapshot => Assert.Equal(AllyId, Assert.Single(snapshot).OtherPlayerId));
        Assert.Equal(repository.Snapshots[0], repository.Snapshots[1]);
    }

    private static EncounterMatch Match(
        DateTimeOffset occurredAt,
        bool ownerWon,
        params (Guid PlayerId, int TeamId)[] others) =>
        new(
            Guid.NewGuid(),
            occurredAt,
            OwnerId,
            100,
            ownerWon,
            [new EncounterParticipant(OwnerId, 100), .. others.Select(item => new EncounterParticipant(item.PlayerId, item.TeamId))]);

    private sealed class RecordingAnalysisRepository(PlayerAnalysisInput input) : IPlayerAnalysisRepository
    {
        public int ReplacementCount { get; private set; }

        public List<IReadOnlyCollection<PlayerEncounterAggregate>> Snapshots { get; } = [];

        public Task<PlayerAnalysisInput?> LoadInputAsync(string ownerPuuid, CancellationToken cancellationToken) =>
            Task.FromResult<PlayerAnalysisInput?>(input);

        public Task<MatchFamiliarityInput?> LoadFamiliarityInputAsync(
            string ownerPuuid,
            string targetRiotMatchId,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task ReplaceEncountersAsync(
            Guid ownerPlayerId,
            IReadOnlyCollection<PlayerEncounterAggregate> encounters,
            CancellationToken cancellationToken)
        {
            ReplacementCount++;
            Snapshots.Add(encounters.ToArray());
            return Task.CompletedTask;
        }

        public Task<PlayerSummary?> GetSummaryAsync(string puuid, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<PlayerEncounterView>?> GetRepeatedPlayersAsync(
            string puuid,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<PagedPlayerMatches?> GetMatchesAsync(
            string puuid,
            int page,
            int pageSize,
            int? queueId,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<MatchDetail?> GetMatchDetailAsync(string riotMatchId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
