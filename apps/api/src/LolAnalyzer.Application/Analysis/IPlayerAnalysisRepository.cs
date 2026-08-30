namespace LolAnalyzer.Application.Analysis;

public interface IPlayerAnalysisRepository
{
    Task<PlayerAnalysisInput?> LoadInputAsync(string ownerPuuid, CancellationToken cancellationToken);

    Task ReplaceEncountersAsync(
        Guid ownerPlayerId,
        IReadOnlyCollection<PlayerEncounterAggregate> encounters,
        CancellationToken cancellationToken);

    Task<PlayerSummary?> GetSummaryAsync(string puuid, CancellationToken cancellationToken);

    Task<IReadOnlyList<PlayerEncounterView>?> GetRepeatedPlayersAsync(
        string puuid,
        CancellationToken cancellationToken);

    Task<PagedPlayerMatches?> GetMatchesAsync(
        string puuid,
        int page,
        int pageSize,
        int? queueId,
        CancellationToken cancellationToken);

    Task<MatchDetail?> GetMatchDetailAsync(string riotMatchId, CancellationToken cancellationToken);
}
