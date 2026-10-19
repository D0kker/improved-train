namespace LolAnalyzer.Application.Analysis;

public sealed class PlayerRelationshipAnalysisService(
    IPlayerRelationshipRepository repository,
    PlayerRelationshipAnalyzer analyzer,
    PlayerRelationshipAnalysisOptions options)
{
    public async Task<PlayerRelationshipAnalysisResult> UpdateIncrementallyAsync(
        IReadOnlyCollection<string> changedRiotMatchIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(changedRiotMatchIds);
        var changedIds = changedRiotMatchIds
            .Where(matchId => !string.IsNullOrWhiteSpace(matchId))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        if (changedIds.Length == 0)
        {
            return new PlayerRelationshipAnalysisResult(0, 0);
        }

        await using var update = await repository
            .BeginIncrementalUpdateAsync(changedIds, options.ReadBatchSize, cancellationToken)
            .ConfigureAwait(false);
        var affectedPairs = update.Input.AffectedPairs.ToHashSet();
        var relationships = analyzer
            .Analyze(update.Input.Matches)
            .Where(relationship => affectedPairs.Contains(new PlayerRelationshipPair(
                relationship.PlayerAId,
                relationship.PlayerBId)))
            .ToArray();
        await update
            .UpsertRelationshipsAsync(relationships, options.WriteBatchSize, cancellationToken)
            .ConfigureAwait(false);
        await update.CommitAsync(cancellationToken).ConfigureAwait(false);

        return new PlayerRelationshipAnalysisResult(update.Input.Matches.Count, relationships.Length);
    }
}
