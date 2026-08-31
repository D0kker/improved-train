namespace LolAnalyzer.Application.Analysis;

public sealed class PlayerRelationshipQueryService(
    IPlayerRelationshipRepository repository,
    PossiblePremadeDetector premadeDetector)
{
    public async Task<PagedPlayerRelationships?> GetAsync(
        string puuid,
        int page,
        int pageSize,
        RelationshipConfidence minimumConfidence,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(puuid);
        var result = await repository
            .GetRelationshipsAsync(puuid, page, pageSize, minimumConfidence, cancellationToken)
            .ConfigureAwait(false);
        if (result is null)
        {
            return null;
        }

        return new PagedPlayerRelationships(
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.Items.Select(ToView).ToArray());
    }

    private PlayerRelationshipView ToView(PlayerRelationshipQueryItem relationship)
    {
        var detection = premadeDetector.Detect(new PremadeDetectionInput(
            relationship.MatchesTogether,
            relationship.SameTeamMatches,
            relationship.RelationshipConfidence));

        return new PlayerRelationshipView(
            relationship.OtherPlayerPuuid,
            relationship.GameName,
            relationship.TagLine,
            relationship.MatchesTogether,
            relationship.SameTeamMatches,
            relationship.OppositeTeamMatches,
            detection.SameTeamRatio,
            relationship.RecentMatchesTogether,
            relationship.ConsecutiveMatches,
            relationship.FirstSeenAt,
            relationship.LastSeenAt,
            relationship.RelationshipScore,
            relationship.RelationshipConfidence.ToLabel(),
            detection.Label);
    }
}
