using LolAnalyzer.Application.Analysis;
using LolAnalyzer.Application.Caching;
using LolAnalyzer.Application.Matches;
using LolAnalyzer.Application.Observability;
using LolAnalyzer.Application.Riot;
using LolAnalyzer.Infrastructure.Caching;
using LolAnalyzer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LolAnalyzer.UnitTests;

public sealed class InMemoryStatisticsTests
{
    private static readonly string[] SharedPuuids = ["owner", "shared-a", "shared-b"];
    [Fact]
    public async Task FiveMatchesDistinguishParticipationsUniquePlayersAndPriorEncounters()
    {
        await using var db = new LolAnalyzerDbContext(Options());
        var matches = new MatchRepository(db);
        for (var index = 0; index < 5; index++)
        {
            Assert.True(await matches.SaveIfMissingAsync(Match(index), "LA1", TestContext.Current.CancellationToken));
        }

        var repository = new PlayerAnalysisRepository(db);
        // The summary is independent of a potentially stale encounter snapshot.
        var summary = await repository.GetSummaryAsync("owner", TestContext.Current.CancellationToken);
        Assert.NotNull(summary);
        Assert.Equal(5, summary.MatchesAnalyzed);
        Assert.Equal(3, summary.Wins);
        Assert.Equal(2, summary.Losses);
        Assert.Equal(60, summary.WinRate);
        Assert.Equal(45, summary.OtherPlayerParticipations);
        Assert.Equal(37, summary.UniquePlayersEncountered);
        Assert.Equal(2, summary.RepeatedPlayers);

        var rebuild = new RepeatedPlayerAnalysisService(repository);
        await rebuild.RebuildAsync("owner", TestContext.Current.CancellationToken);
        await rebuild.RebuildAsync("owner", TestContext.Current.CancellationToken);
        var encounters = await repository.GetKnownPlayersAsync("owner", TestContext.Current.CancellationToken);
        Assert.NotNull(encounters);
        Assert.Equal(37, encounters.Count);
        Assert.Equal(45, encounters.Sum(encounter => encounter.TotalMatches));

        var input = await repository.LoadFamiliarityInputAsync("owner", "TEST_4", TestContext.Current.CancellationToken);
        var familiarity = MatchFamiliarityCalculator.Calculate(Assert.IsType<MatchFamiliarityInput>(input));
        Assert.Equal(MatchFamiliarityStatus.Available, familiarity.Status);
        Assert.Equal(9, familiarity.EvaluablePlayers);
        Assert.Equal(2, familiarity.KnownPlayers);
        Assert.Equal(7, familiarity.UnknownPlayers);
        Assert.Equal(22.2m, familiarity.FamiliarityPercentage);
        var shared = Assert.Single(familiarity.KnownPlayerDetails, player => player.Puuid == "shared-a");
        Assert.Equal(4, shared.PlayedBefore);
        Assert.Equal(2, shared.AllyMatches);
        Assert.Equal(2, shared.EnemyMatches);
        Assert.Equal(1, shared.WinsTogether);
        Assert.Equal(1, shared.LossesTogether);
        Assert.Equal(1, shared.WinsAgainst);
        Assert.Equal(1, shared.LossesAgainst);

        var relationships = new PlayerRelationshipRepository(db,
            new PlayerRelationshipAnalyzer(new RelationshipScoreCalculator(new RelationshipScoreOptions())));
        var earliest = await relationships.LoadMatchPremadeGroupInputAsync("TEST_0", TestContext.Current.CancellationToken);
        Assert.NotNull(earliest);
        Assert.Empty(earliest.Relationships);
        var historical = await relationships.LoadMatchPremadeGroupInputAsync("TEST_2", TestContext.Current.CancellationToken);
        Assert.NotNull(historical);
        Assert.All(historical.Relationships, pair => Assert.Equal(2, pair.MatchesTogether));
    }

    [Fact]
    public async Task ConcurrentSharedMatchesKeepOneMatchAndOneIdentityPerPuuid()
    {
        var options = Options();
        var token = TestContext.Current.CancellationToken;
        var saves = await Task.WhenAll(Enumerable.Range(0, 16).Select(index => Task.Run(async () =>
        {
            await using var db = new LolAnalyzerDbContext(options);
            return await new MatchRepository(db).SaveIfMissingAsync(Match(index % 2), "LA1", token);
        }, token)));
        await using var read = new LolAnalyzerDbContext(options);
        Assert.Equal(2, saves.Count(saved => saved));
        Assert.Equal(2, await read.Matches.CountAsync(token));
        Assert.Equal(17, await read.Players.CountAsync(token));
        Assert.Equal(20, await read.MatchParticipants.CountAsync(token));
        Assert.All(await read.Players.GroupBy(player => player.Puuid).Select(group => group.Count()).ToArrayAsync(token),
            count => Assert.Equal(1, count));
    }

    [Fact]
    public async Task FailedDetailsKeepSuccessfulEvidenceAndExposeAnIncompleteResult()
    {
        await using var db = new LolAnalyzerDbContext(Options());
        var ingestion = new MatchIngestionService(new FixtureRiotClient(failSecond: true), new MatchRepository(db),
            new MatchIngestionOptions(), new OperationalMetrics());
        var result = await ingestion.SynchronizeAsync("owner", 2, "LA1", TestContext.Current.CancellationToken);
        Assert.Equal(2, result.MatchIdsReturned);
        Assert.Equal(1, result.Persisted);
        Assert.Equal(1, result.Failed);
        Assert.Equal(0, result.NotFound);
    }

    [Fact]
    public async Task IngestingAnotherPlayersHistoryInvalidatesAffectedPlayerSummary()
    {
        await using var db = new LolAnalyzerDbContext(Options());
        var repository = new MatchRepository(db);
        await repository.SaveIfMissingAsync(Match(0), "LA1", TestContext.Current.CancellationToken);
        var cache = new MemoryCacheService(TimeProvider.System);
        var summary = new PlayerSummaryQueryService(new PlayerAnalysisRepository(db), cache, new CacheOptions());
        Assert.Equal(1, (await summary.GetAsync("owner", TestContext.Current.CancellationToken))!.MatchesAnalyzed);
        var ingestion = new MatchIngestionService(new FixtureRiotClient(), repository,
            new MatchIngestionOptions(), new OperationalMetrics(), cache);
        await ingestion.SynchronizeAsync("shared-a", 1, "LA1", TestContext.Current.CancellationToken);
        Assert.Equal(2, (await summary.GetAsync("owner", TestContext.Current.CancellationToken))!.MatchesAnalyzed);
    }

    private static DbContextOptions<LolAnalyzerDbContext> Options() =>
        new DbContextOptionsBuilder<LolAnalyzerDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;

    private static RiotMatchData Match(int index)
    {
        var players = SharedPuuids
            .Concat(Enumerable.Range(0, 7).Select(number => $"other-{index}-{number}"));
        var participants = players.Select((puuid, number) => new RiotMatchParticipantData(
            puuid, puuid, "TEST", number == 0 || number < 5 && (number > 2 || index < 2) ? 100 : 200,
            number + 1, 1, "TestChampion", null, null, 0, 0, 0,
            number == 0 ? index % 2 == 0 : false, 0, 0, 0, 0)).ToArray();
        return new RiotMatchData($"TEST_{index}", 420, null, DateTimeOffset.UnixEpoch.AddDays(index),
            DateTimeOffset.UnixEpoch.AddDays(index).AddMinutes(30), 1800, "test", "{}", participants);
    }

    private sealed class FixtureRiotClient(bool failSecond = false) : IRiotApiClient
    {
        public Task<RiotAccount?> GetAccountByRiotIdAsync(string gameName, string tagLine, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<string>> GetMatchIdsAsync(string puuid, int start, int count, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<string>>(failSecond ? ["TEST_1", "TEST_FAIL"] : ["TEST_1"]);

        public Task<RiotMatchData?> GetMatchAsync(string riotMatchId, CancellationToken cancellationToken) =>
            riotMatchId == "TEST_FAIL"
                ? throw new RiotApiException(System.Net.HttpStatusCode.ServiceUnavailable)
                : Task.FromResult<RiotMatchData?>(Match(1));
    }
}
