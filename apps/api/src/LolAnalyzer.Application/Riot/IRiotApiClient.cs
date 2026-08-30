namespace LolAnalyzer.Application.Riot;

public interface IRiotApiClient
{
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
}
