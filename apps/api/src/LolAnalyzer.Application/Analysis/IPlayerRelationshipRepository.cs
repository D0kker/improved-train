namespace LolAnalyzer.Application.Analysis;

public interface IPlayerRelationshipRepository
{
    Task<IReadOnlyList<RelationshipMatchSnapshot>> LoadMatchesAsync(
        int batchSize,
        CancellationToken cancellationToken);

    Task ReplaceRelationshipsAsync(
        IReadOnlyCollection<PlayerRelationshipAggregate> relationships,
        int batchSize,
        CancellationToken cancellationToken);

    Task<PagedPlayerRelationshipQuery?> GetRelationshipsAsync(
        string puuid,
        int page,
        int pageSize,
        RelationshipConfidence minimumConfidence,
        int minimumScore,
        CancellationToken cancellationToken);

    Task<MatchPremadeGroupInput?> LoadMatchPremadeGroupInputAsync(
        string riotMatchId,
        CancellationToken cancellationToken);
}
