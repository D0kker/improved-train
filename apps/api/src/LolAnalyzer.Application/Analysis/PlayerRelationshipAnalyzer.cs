namespace LolAnalyzer.Application.Analysis;

public sealed class PlayerRelationshipAnalyzer(RelationshipScoreCalculator scoreCalculator)
{
    public IReadOnlyList<PlayerRelationshipAggregate> Analyze(
        IReadOnlyCollection<RelationshipMatchSnapshot> matches)
    {
        ArgumentNullException.ThrowIfNull(matches);
        if (matches.Count == 0)
        {
            return [];
        }

        var uniqueMatches = ValidateAndOrderMatches(matches);
        var evaluatedAt = uniqueMatches[^1].OccurredAt;
        var matchesByPlayer = BuildPlayerTimelines(uniqueMatches);
        var pairs = BuildCandidatePairs(uniqueMatches);

        return pairs
            .OrderBy(pair => pair.PlayerAId)
            .ThenBy(pair => pair.PlayerBId)
            .Select(pair => AnalyzePair(pair, matchesByPlayer, evaluatedAt))
            .ToArray();
    }

    private PlayerRelationshipAggregate AnalyzePair(
        PlayerPair pair,
        Dictionary<Guid, IReadOnlyList<RelationshipMatchSnapshot>> matchesByPlayer,
        DateTimeOffset evaluatedAt)
    {
        var timeline = matchesByPlayer[pair.PlayerAId]
            .Concat(matchesByPlayer[pair.PlayerBId])
            .DistinctBy(match => match.MatchId, StringComparer.Ordinal)
            .OrderBy(match => match.OccurredAt)
            .ThenBy(match => match.MatchId, StringComparer.Ordinal)
            .Select(match => ToEvidence(match, pair))
            .ToArray();
        var encounters = timeline.Where(match => match.Encountered).ToArray();
        var score = scoreCalculator.Calculate(new RelationshipScoreInput(evaluatedAt, timeline));
        var sameTeamMatches = encounters.Count(match => match.SameTeam);

        return new PlayerRelationshipAggregate(
            pair.PlayerAId,
            pair.PlayerBId,
            encounters.Length,
            sameTeamMatches,
            encounters.Length - sameTeamMatches,
            score.Factors.RecentFrequency.EvidenceCount,
            score.Factors.ConsecutiveMatches.EvidenceCount,
            encounters[0].OccurredAt,
            encounters[^1].OccurredAt,
            score.Score,
            score.ConfidenceLabel);
    }

    private static RelationshipMatchEvidence ToEvidence(RelationshipMatchSnapshot match, PlayerPair pair)
    {
        var playerA = match.Participants.SingleOrDefault(participant => participant.PlayerId == pair.PlayerAId);
        var playerB = match.Participants.SingleOrDefault(participant => participant.PlayerId == pair.PlayerBId);
        var encountered = playerA is not null && playerB is not null;

        return new RelationshipMatchEvidence(
            match.MatchId,
            match.OccurredAt,
            encountered,
            encountered && playerA!.TeamId == playerB!.TeamId);
    }

    private static RelationshipMatchSnapshot[] ValidateAndOrderMatches(
        IReadOnlyCollection<RelationshipMatchSnapshot> matches)
    {
        var matchIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var match in matches)
        {
            if (string.IsNullOrWhiteSpace(match.MatchId) || !matchIds.Add(match.MatchId))
            {
                throw new ArgumentException("Relationship analysis requires unique, non-empty match IDs.", nameof(matches));
            }

            if (match.Participants.Select(participant => participant.PlayerId).Distinct().Count()
                != match.Participants.Count)
            {
                throw new ArgumentException("A match cannot contain a player more than once.", nameof(matches));
            }
        }

        return matches
            .OrderBy(match => match.OccurredAt)
            .ThenBy(match => match.MatchId, StringComparer.Ordinal)
            .ToArray();
    }

    private static Dictionary<Guid, IReadOnlyList<RelationshipMatchSnapshot>> BuildPlayerTimelines(
        IReadOnlyList<RelationshipMatchSnapshot> matches)
    {
        var timelines = new Dictionary<Guid, List<RelationshipMatchSnapshot>>();
        foreach (var match in matches)
        {
            foreach (var participant in match.Participants)
            {
                if (!timelines.TryGetValue(participant.PlayerId, out var timeline))
                {
                    timeline = [];
                    timelines.Add(participant.PlayerId, timeline);
                }

                timeline.Add(match);
            }
        }

        return timelines.ToDictionary(
            item => item.Key,
            item => (IReadOnlyList<RelationshipMatchSnapshot>)item.Value);
    }

    private static HashSet<PlayerPair> BuildCandidatePairs(IReadOnlyList<RelationshipMatchSnapshot> matches)
    {
        var pairs = new HashSet<PlayerPair>();
        foreach (var match in matches)
        {
            var playerIds = match.Participants
                .Select(participant => participant.PlayerId)
                .Order()
                .ToArray();
            for (var first = 0; first < playerIds.Length; first++)
            {
                for (var second = first + 1; second < playerIds.Length; second++)
                {
                    pairs.Add(new PlayerPair(playerIds[first], playerIds[second]));
                }
            }
        }

        return pairs;
    }

    private sealed record PlayerPair(Guid PlayerAId, Guid PlayerBId);
}
