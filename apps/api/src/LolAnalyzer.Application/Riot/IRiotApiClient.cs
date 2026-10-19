namespace LolAnalyzer.Application.Riot;

public interface IRiotApiClient
{
    IRiotApiClient ForPlatform(string platformRegion) => this;

    Task<IReadOnlyList<string>> GetMatchIdsBeforeAsync(
        string puuid,
        int count,
        long endTime,
        CancellationToken cancellationToken) =>
        GetMatchIdsAsync(puuid, 0, count, cancellationToken);

    Task<RiotAccount?> GetAccountByRiotIdAsync(
        string gameName,
        string tagLine,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> GetMatchIdsAsync(
        string puuid,
        int start,
        int count,
        CancellationToken cancellationToken);

    Task<RiotMatchData?> GetMatchAsync(
        string riotMatchId,
        CancellationToken cancellationToken);

    Task<RiotActiveGame?> GetActiveGameAsync(
        string puuid,
        CancellationToken cancellationToken) =>
        Task.FromResult<RiotActiveGame?>(null);
}
