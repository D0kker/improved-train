using LolAnalyzer.Application.Players;
using LolAnalyzer.Application.Riot;
using LolAnalyzer.Domain.Entities;
using Xunit;

namespace LolAnalyzer.UnitTests;

public sealed class PlayerLookupServiceTests
{
    [Fact]
    public async Task KnownRiotIdUsesLocalPlayerWithoutCallingRiot()
    {
        var local = Player("local-puuid", "Ana", "LAN");
        var riot = new RecordingRiotClient(new RiotAccount("remote-puuid", "Ana", "LAN"));
        var repository = new RecordingPlayerRepository(local);
        var service = new PlayerLookupService(riot, repository);

        var result = await service.FindByRiotIdAsync(
            "Ana",
            "LAN",
            "la1",
            TestContext.Current.CancellationToken);

        Assert.Same(local, result);
        Assert.Equal(0, riot.AccountRequests);
        Assert.Equal(0, repository.Upserts);
    }

    [Fact]
    public async Task UnknownRiotIdResolvesAndPersistsExactlyOnce()
    {
        var riot = new RecordingRiotClient(new RiotAccount("remote-puuid", "Bea", "LAN"));
        var repository = new RecordingPlayerRepository(local: null);
        var service = new PlayerLookupService(riot, repository);

        var result = await service.FindByRiotIdAsync(
            "Bea",
            "LAN",
            "la1",
            TestContext.Current.CancellationToken);

        Assert.Equal("remote-puuid", result?.Puuid);
        Assert.Equal(1, riot.AccountRequests);
        Assert.Equal(1, repository.Upserts);
    }

    private static Player Player(string puuid, string gameName, string tagLine) => new()
    {
        Puuid = puuid,
        GameName = gameName,
        TagLine = tagLine,
        PlatformRegion = "la1",
    };

    private sealed class RecordingPlayerRepository(Player? local) : IPlayerRepository
    {
        public int Upserts { get; private set; }

        public Task<Player?> FindByRiotIdAsync(
            string gameName,
            string tagLine,
            CancellationToken cancellationToken) => Task.FromResult(local);

        public Task<Player> UpsertAsync(
            string puuid,
            string gameName,
            string tagLine,
            string platformRegion,
            CancellationToken cancellationToken)
        {
            Upserts++;
            return Task.FromResult(Player(puuid, gameName, tagLine));
        }
    }

    private sealed class RecordingRiotClient(RiotAccount? account) : IRiotApiClient
    {
        public int AccountRequests { get; private set; }

        public Task<RiotAccount?> GetAccountByRiotIdAsync(
            string gameName,
            string tagLine,
            CancellationToken cancellationToken)
        {
            AccountRequests++;
            return Task.FromResult(account);
        }

        public Task<IReadOnlyList<string>> GetMatchIdsAsync(
            string puuid,
            int start,
            int count,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<RiotMatchData?> GetMatchAsync(
            string riotMatchId,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
