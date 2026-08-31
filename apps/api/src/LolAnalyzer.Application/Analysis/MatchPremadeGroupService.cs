namespace LolAnalyzer.Application.Analysis;

public sealed class MatchPremadeGroupService(
    IPlayerRelationshipRepository repository,
    PossiblePremadeDetector pairDetector,
    PossiblePremadeGroupDetector groupDetector)
{
    public async Task<IReadOnlyList<MatchPremadeGroup>?> GetAsync(
        string riotMatchId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(riotMatchId);
        var input = await repository
            .LoadMatchPremadeGroupInputAsync(riotMatchId, cancellationToken)
            .ConfigureAwait(false);
        if (input is null)
        {
            return null;
        }

        var participants = input.Participants
            .Where(participant =>
                !string.IsNullOrWhiteSpace(participant.Puuid)
                && !string.IsNullOrWhiteSpace(participant.GameName)
                && !string.IsNullOrWhiteSpace(participant.TagLine))
            .ToDictionary(participant => participant.PlayerId);
        var detectedPairs = input.Relationships
            .Where(relationship =>
                participants.TryGetValue(relationship.PlayerAId, out var first)
                && participants.TryGetValue(relationship.PlayerBId, out var second)
                && first.TeamId == second.TeamId)
            .Select(relationship => new
            {
                Pair = relationship,
                Classification = pairDetector.Detect(new PremadeDetectionInput(
                    relationship.MatchesTogether,
                    relationship.SameTeamMatches,
                    relationship.RelationshipConfidence)).Classification,
            })
            .Where(result => result.Classification is not PremadeClassification.NoEvidence)
            .ToArray();

        var detectedGroups = participants.Values
            .Select(participant => participant.TeamId)
            .Distinct()
            .Order()
            .SelectMany(teamId => DetectTeamGroups(
                teamId,
                detectedPairs
                    .Where(result => participants[result.Pair.PlayerAId].TeamId == teamId)
                    .Select(result => new PremadeGroupPair(
                        result.Pair.PlayerAId,
                        result.Pair.PlayerBId,
                        result.Classification))
                    .ToArray()))
            .OrderBy(result => result.TeamId)
            .ThenByDescending(result => result.Group.PlayerIds.Count)
            .ThenByDescending(result => result.Group.Classification)
            .ThenBy(result => string.Join(':', result.Group.PlayerIds))
            .ToArray();

        return detectedGroups.Select((result, index) => new MatchPremadeGroup(
            index + 1,
            result.TeamId,
            result.Group.Classification.ToString(),
            result.Group.Classification == PremadeClassification.LikelyPremade
                ? "possible premade · high evidence"
                : "possible premade",
            result.Group.PlayerIds.Select(playerId =>
            {
                var participant = participants[playerId];
                return new MatchPremadeGroupMember(
                    participant.Puuid,
                    participant.GameName,
                    participant.TagLine);
            }).ToArray())).ToArray();
    }

    private TeamPremadeGroup[] DetectTeamGroups(
        int teamId,
        IReadOnlyCollection<PremadeGroupPair> pairs)
    {
        var largerGroups = groupDetector.Detect(pairs);
        var uncoveredPairs = pairs
            .Where(pair => !largerGroups.Any(group =>
                group.PlayerIds.Contains(pair.PlayerAId)
                && group.PlayerIds.Contains(pair.PlayerBId)))
            .Select(pair => new PossiblePremadeGroup(
                [pair.PlayerAId, pair.PlayerBId],
                pair.Classification));

        return largerGroups
            .Concat(uncoveredPairs)
            .Select(group => new TeamPremadeGroup(teamId, group))
            .ToArray();
    }

    private sealed record TeamPremadeGroup(int TeamId, PossiblePremadeGroup Group);
}
