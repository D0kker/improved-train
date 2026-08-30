namespace LolAnalyzer.Application.Analysis;

public static class RepeatedPlayerAnalyzer
{
    public static IReadOnlyList<PlayerEncounterAggregate> Analyze(PlayerAnalysisInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var aggregates = new Dictionary<Guid, MutableEncounter>();
        foreach (var match in input.Matches
                     .Where(match => match.OwnerPlayerId == input.OwnerPlayerId)
                     .OrderBy(match => match.OccurredAt)
                     .ThenBy(match => match.MatchId))
        {
            foreach (var participant in match.Participants
                         .Where(participant => participant.PlayerId != input.OwnerPlayerId)
                         .DistinctBy(participant => participant.PlayerId))
            {
                if (!aggregates.TryGetValue(participant.PlayerId, out var encounter))
                {
                    encounter = new MutableEncounter(match.OccurredAt);
                    aggregates.Add(participant.PlayerId, encounter);
                }

                encounter.Add(
                    sameTeam: participant.TeamId == match.OwnerTeamId,
                    ownerWon: match.OwnerWon,
                    occurredAt: match.OccurredAt);
            }
        }

        return aggregates
            .Select(pair => pair.Value.ToAggregate(pair.Key))
            .OrderByDescending(encounter => encounter.TotalMatches)
            .ThenBy(encounter => encounter.OtherPlayerId)
            .ToArray();
    }

    private sealed class MutableEncounter(DateTimeOffset firstSeenAt)
    {
        private int _totalMatches;
        private int _sameTeamMatches;
        private int _enemyTeamMatches;
        private int _winsTogether;
        private int _lossesTogether;
        private int _winsAgainst;
        private int _lossesAgainst;
        private DateTimeOffset _firstSeenAt = firstSeenAt;
        private DateTimeOffset _lastSeenAt = firstSeenAt;

        public void Add(bool sameTeam, bool ownerWon, DateTimeOffset occurredAt)
        {
            _totalMatches++;
            _firstSeenAt = occurredAt < _firstSeenAt ? occurredAt : _firstSeenAt;
            _lastSeenAt = occurredAt > _lastSeenAt ? occurredAt : _lastSeenAt;

            if (sameTeam)
            {
                _sameTeamMatches++;
                if (ownerWon)
                {
                    _winsTogether++;
                }
                else
                {
                    _lossesTogether++;
                }

                return;
            }

            _enemyTeamMatches++;
            if (ownerWon)
            {
                _winsAgainst++;
            }
            else
            {
                _lossesAgainst++;
            }
        }

        public PlayerEncounterAggregate ToAggregate(Guid otherPlayerId) => new(
            otherPlayerId,
            _totalMatches,
            _sameTeamMatches,
            _enemyTeamMatches,
            _winsTogether,
            _lossesTogether,
            _winsAgainst,
            _lossesAgainst,
            _firstSeenAt,
            _lastSeenAt);
    }
}
