namespace LolAnalyzer.Application.Analysis;

public interface IPlayerRelationshipRepository
{
    Task<IPlayerRelationshipUpdateScope> BeginIncrementalUpdateAsync(
        IReadOnlyCollection<string> changedRiotMatchIds,
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

public interface IPlayerRelationshipUpdateScope : IAsyncDisposable
{
    IncrementalRelationshipAnalysisInput Input { get; }

    Task UpsertRelationshipsAsync(
        IReadOnlyCollection<PlayerRelationshipAggregate> relationships,
        int batchSize,
        CancellationToken cancellationToken);

    Task CommitAsync(CancellationToken cancellationToken);
}
