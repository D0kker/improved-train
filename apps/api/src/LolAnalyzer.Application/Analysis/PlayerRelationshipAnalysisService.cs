namespace LolAnalyzer.Application.Analysis;

public sealed class PlayerRelationshipAnalysisService(
    IPlayerRelationshipRepository repository,
    PlayerRelationshipAnalyzer analyzer,
    PlayerRelationshipAnalysisOptions options)
{
    public async Task<PlayerRelationshipAnalysisResult> RebuildAsync(CancellationToken cancellationToken)
    {
        var matches = await repository
            .LoadMatchesAsync(options.ReadBatchSize, cancellationToken)
            .ConfigureAwait(false);
        var relationships = analyzer.Analyze(matches);
        await repository
            .ReplaceRelationshipsAsync(relationships, options.WriteBatchSize, cancellationToken)
            .ConfigureAwait(false);

        return new PlayerRelationshipAnalysisResult(matches.Count, relationships.Count);
    }
}
