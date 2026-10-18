namespace LolAnalyzer.Application.Analysis;

public sealed class MatchPremadeGroupService(
    IPlayerRelationshipRepository repository,
    IPlayerAnalysisRepository analysisRepository,
    PossiblePremadeDetector pairDetector,
    PossiblePremadeGroupDetector groupDetector,
    PremadeDetectionOptions options)
{
    public async Task<IReadOnlyList<MatchPremadeGroup>?> GetAsync(
        string riotMatchId,
        string? ownerPuuid,
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

        var recentEvidence = string.IsNullOrWhiteSpace(ownerPuuid)
            ? new Dictionary<(Guid, Guid), RecentPairEvidence>()
            : await LoadRecentEvidenceAsync(ownerPuuid, input, cancellationToken)
                .ConfigureAwait(false);

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
                    relationship.RelationshipConfidence,
                    recentEvidence.GetValueOrDefault((relationship.PlayerAId, relationship.PlayerBId))?.OppositeTeamMatches ?? 0,
                    recentEvidence.GetValueOrDefault((relationship.PlayerAId, relationship.PlayerBId))?.DistinctSameTeamDates ?? 0)).Classification,
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

    private async Task<Dictionary<(Guid PlayerAId, Guid PlayerBId), RecentPairEvidence>> LoadRecentEvidenceAsync(
        string ownerPuuid,
        MatchPremadeGroupInput input,
        CancellationToken cancellationToken)
    {
        var participantsByPuuid = input.Participants.ToDictionary(participant => participant.Puuid);
        var matches = await analysisRepository
            .GetMatchesAsync(ownerPuuid, 1, options.RecentOwnerMatchCount, null, cancellationToken)
            .ConfigureAwait(false);
        var evidence = new Dictionary<(Guid, Guid), RecentPairEvidence>();
        if (matches is null)
        {
            return evidence;
        }

        foreach (var item in matches.Items)
        {
            var detail = await analysisRepository
                .GetMatchDetailAsync(item.RiotMatchId, cancellationToken)
                .ConfigureAwait(false);
            if (detail is null)
            {
                continue;
            }

            var teams = detail.Teams
                .SelectMany(team => team.Participants.Select(participant => (participant.Puuid, team.TeamId)))
                .Where(entry => participantsByPuuid.ContainsKey(entry.Puuid))
                .ToDictionary(entry => entry.Puuid, entry => entry.TeamId);
            foreach (var relationship in input.Relationships)
            {
                var first = input.Participants.FirstOrDefault(participant => participant.PlayerId == relationship.PlayerAId);
                var second = input.Participants.FirstOrDefault(participant => participant.PlayerId == relationship.PlayerBId);
                if (first is null || second is null
                    || !teams.TryGetValue(first.Puuid, out var firstTeam)
                    || !teams.TryGetValue(second.Puuid, out var secondTeam))
                {
                    continue;
                }

                var key = (relationship.PlayerAId, relationship.PlayerBId);
                var current = evidence.GetValueOrDefault(key) ?? new RecentPairEvidence();
                var sameTeam = firstTeam == secondTeam;
                if (sameTeam)
                {
                    var dates = current.SameTeamDates is null
                        ? new HashSet<DateOnly>()
                        : [.. current.SameTeamDates];
                    if (detail.GameStartTimestamp is { } occurredAt)
                    {
                        dates.Add(DateOnly.FromDateTime(occurredAt.DateTime));
                    }

                    evidence[key] = current with
                    {
                        SameTeamMatches = current.SameTeamMatches + 1,
                        SameTeamDates = dates,
                    };
                }
                else
                {
                    evidence[key] = current with { OppositeTeamMatches = current.OppositeTeamMatches + 1 };
                }
            }
        }

        return evidence;
    }

    private sealed record RecentPairEvidence(
        int SameTeamMatches = 0,
        int OppositeTeamMatches = 0,
        HashSet<DateOnly>? SameTeamDates = null)
    {
        public int DistinctSameTeamDates => SameTeamDates?.Count ?? 0;
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
