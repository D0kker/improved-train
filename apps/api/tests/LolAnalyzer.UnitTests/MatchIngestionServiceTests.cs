using LolAnalyzer.Application.Matches;
using LolAnalyzer.Application.Observability;
using LolAnalyzer.Application.Riot;
using Xunit;

namespace LolAnalyzer.UnitTests;

public sealed class MatchIngestionServiceTests
{
    private static readonly string[] ReturnedMatchIds = ["TEST_1", "TEST_2", "TEST_3"];

    [Fact]
    public async Task SynchronizeAsyncDownloadsOnlyMatchIdsAbsentFromTheLocalRepository()
    {
        var riotClient = new SimulatedRiotApiClient(ReturnedMatchIds);
        var repository = new SimulatedMatchRepository(new HashSet<string>(["TEST_2"], StringComparer.Ordinal));
        var service = new MatchIngestionService(
            riotClient,
            repository,
            new MatchIngestionOptions { RequestConcurrency = 2 },
            new OperationalMetrics());

        var result = await service.SynchronizeAsync(
            "owner-puuid",
            3,
            "la1",
            TestContext.Current.CancellationToken);

        Assert.Equal(3, result.MatchIdsReturned);
        Assert.Equal(1, result.AlreadyStored);
        Assert.Equal(2, result.Downloaded);
        Assert.Equal(2, result.Persisted);
        Assert.Equal(["TEST_1", "TEST_3"], riotClient.DownloadedMatchIds.OrderBy(matchId => matchId));
        Assert.Equal(["TEST_1", "TEST_3"], repository.SavedMatchIds.OrderBy(matchId => matchId));
    }

    [Fact]
    public async Task TwoPagedBatchesWithOneHundredNinetyStoredMatchesDownloadExactlyTenDetails()
    {
        var allMatchIds = Enumerable.Range(1, 200).Select(index => $"TEST_{index:D3}").ToArray();
        var riotClient = new SimulatedRiotApiClient(allMatchIds, honorPaging: true);
        var repository = new SimulatedMatchRepository(
            allMatchIds.Take(190).ToHashSet(StringComparer.Ordinal));
        var service = new MatchIngestionService(
            riotClient,
            repository,
            new MatchIngestionOptions { RequestConcurrency = 3 },
            new OperationalMetrics());

        var first = await service.SynchronizeAsync(
            "owner-puuid",
            start: 0,
            count: 100,
            platformRegion: "la1",
            cancellationToken: TestContext.Current.CancellationToken);
        var second = await service.SynchronizeAsync(
            "owner-puuid",
            start: 100,
            count: 100,
            platformRegion: "la1",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(100, first.AlreadyStored);
        Assert.Equal(90, second.AlreadyStored);
        Assert.Equal(10, second.Downloaded);
        Assert.Equal(10, riotClient.DownloadedMatchIds.Count);
        Assert.Equal([0, 100], riotClient.RequestedStarts);
    }

    private sealed class SimulatedRiotApiClient(
        IReadOnlyList<string> matchIds,
        bool honorPaging = false) : IRiotApiClient
    {
        public List<string> DownloadedMatchIds { get; } = [];

        public List<int> RequestedStarts { get; } = [];

        public Task<RiotAccount?> GetAccountByRiotIdAsync(string gameName, string tagLine, CancellationToken cancellationToken) =>
            Task.FromResult<RiotAccount?>(null);

        public Task<IReadOnlyList<string>> GetMatchIdsAsync(
            string puuid,
            int start,
            int count,
            CancellationToken cancellationToken)
        {
            RequestedStarts.Add(start);
            return Task.FromResult<IReadOnlyList<string>>(
                honorPaging ? matchIds.Skip(start).Take(count).ToArray() : matchIds);
        }

        public Task<RiotMatchData?> GetMatchAsync(string riotMatchId, CancellationToken cancellationToken)
        {
            lock (DownloadedMatchIds)
            {
                DownloadedMatchIds.Add(riotMatchId);
            }

            return Task.FromResult<RiotMatchData?>(TestMatch(riotMatchId));
        }
    }

    private sealed class SimulatedMatchRepository(IReadOnlySet<string> existingMatchIds) : IMatchRepository
    {
        public List<string> SavedMatchIds { get; } = [];

        public Task<IReadOnlySet<string>> FindExistingRiotMatchIdsAsync(
            IReadOnlyCollection<string> riotMatchIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlySet<string>>(
                riotMatchIds.Where(existingMatchIds.Contains).ToHashSet(StringComparer.Ordinal));

        public Task<bool> SaveIfMissingAsync(RiotMatchData match, string platformRegion, CancellationToken cancellationToken)
        {
            SavedMatchIds.Add(match.RiotMatchId);
            return Task.FromResult(true);
        }
    }

    internal static RiotMatchData TestMatch(string matchId) => new(
        matchId,
        420,
        DateTimeOffset.UnixEpoch,
        DateTimeOffset.UnixEpoch,
        DateTimeOffset.UnixEpoch.AddMinutes(20),
        1200,
        "test",
        "{}",
        [new RiotMatchParticipantData("participant-puuid", "Test", "TAG", 100, 1, 1, "Annie", "MIDDLE", "MIDDLE", 0, 0, 0, false, 0, 0, 0, 0)]);
}
