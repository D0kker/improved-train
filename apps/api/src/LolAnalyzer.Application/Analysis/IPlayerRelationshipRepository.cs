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
}
