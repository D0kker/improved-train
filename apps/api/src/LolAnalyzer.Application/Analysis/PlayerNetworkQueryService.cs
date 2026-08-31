namespace LolAnalyzer.Application.Analysis;

public sealed class PlayerNetworkQueryService(
    PlayerRelationshipQueryService relationshipQueryService,
    PlayerNetworkOptions options)
{
    public async Task<PlayerNetwork?> GetAsync(
        string puuid,
        int maximumNodes,
        int maximumEdges,
        RelationshipConfidence minimumConfidence,
        int minimumScore,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(puuid);

        var relationshipLimit = Math.Min(maximumNodes - 1, maximumEdges);
        var relationships = await relationshipQueryService
            .GetAsync(
                puuid,
                1,
                Math.Max(1, relationshipLimit),
                minimumConfidence,
                minimumScore,
                cancellationToken)
            .ConfigureAwait(false);
        if (relationships is null)
        {
            return null;
        }

        var selected = relationshipLimit == 0
            ? []
            : relationships.Items.Take(relationshipLimit).ToArray();
        var center = new PlayerNetworkNode(
            relationships.PlayerPuuid,
            relationships.PlayerGameName,
            relationships.PlayerTagLine,
            true);
        var nodes = new[] { center }
            .Concat(selected.Select(item => new PlayerNetworkNode(
                item.OtherPlayerPuuid,
                item.GameName,
                item.TagLine,
                false)))
            .ToArray();
        var edges = selected.Select(item => new PlayerNetworkEdge(
            center.Puuid,
            item.OtherPlayerPuuid,
            item.MatchesTogether,
            item.SameTeamMatches,
            item.OppositeTeamMatches,
            item.SameTeamRatio,
            item.RelationshipScore,
            item.RelationshipConfidence,
            item.PremadeLabel)).ToArray();
        var totalAvailableEdges = relationships.TotalCount;
        var totalAvailableNodes = totalAvailableEdges + 1;

        return new PlayerNetwork(
            center,
            nodes,
            edges,
            new PlayerNetworkMetadata(
                1,
                totalAvailableNodes > maximumNodes || totalAvailableEdges > maximumEdges,
                totalAvailableNodes,
                totalAvailableEdges,
                maximumNodes,
                maximumEdges));
    }

    public bool LimitsAreValid(int maximumNodes, int maximumEdges) =>
        maximumNodes >= 1
        && maximumNodes <= options.MaximumNodes
        && maximumEdges >= 1
        && maximumEdges <= options.MaximumEdges;
}
