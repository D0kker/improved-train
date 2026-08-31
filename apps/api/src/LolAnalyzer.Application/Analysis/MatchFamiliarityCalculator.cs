namespace LolAnalyzer.Application.Analysis;

public static class MatchFamiliarityCalculator
{
    public static MatchFamiliarityResult Calculate(MatchFamiliarityInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.TargetRiotMatchId);

        var duplicateMatch = input.Matches
            .GroupBy(match => match.RiotMatchId, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateMatch is not null)
        {
            throw new ArgumentException(
                $"The match id '{duplicateMatch.Key}' appears more than once.",
                nameof(input));
        }

        var target = input.Matches.SingleOrDefault(
            match => string.Equals(match.RiotMatchId, input.TargetRiotMatchId, StringComparison.Ordinal));
        if (target is null)
        {
            throw new ArgumentException("The target match is not present in the input.", nameof(input));
        }

        if (!ContainsPlayer(target, input.OwnerPlayerId))
        {
            return EmptyResult(input.TargetRiotMatchId, MatchFamiliarityStatus.OwnerNotPresent);
        }

        var targetPlayerIds = EvaluatablePlayerIds(target, input.OwnerPlayerId);
        if (targetPlayerIds.Length == 0)
        {
            return EmptyResult(input.TargetRiotMatchId, MatchFamiliarityStatus.NoEvaluableParticipants);
        }

        var priorMatches = input.Matches
            .Where(match => ContainsPlayer(match, input.OwnerPlayerId))
            .Where(match => IsBefore(match, target))
            .ToArray();
        var previouslySeenPlayerIds = priorMatches
            .SelectMany(match => EvaluatablePlayerIds(match, input.OwnerPlayerId))
            .ToHashSet();
        var knownPlayerIds = targetPlayerIds
            .Where(previouslySeenPlayerIds.Contains)
            .Order()
            .ToArray();
        var knownPlayers = knownPlayerIds.Length;
        var evaluablePlayers = targetPlayerIds.Length;

        return new MatchFamiliarityResult(
            input.TargetRiotMatchId,
            knownPlayers,
            evaluablePlayers - knownPlayers,
            evaluablePlayers,
            Math.Round(knownPlayers * 100m / evaluablePlayers, 1, MidpointRounding.AwayFromZero),
            priorMatches.Length == 0 ? MatchFamiliarityStatus.NoPriorHistory : MatchFamiliarityStatus.Available,
            knownPlayerIds);
    }

    private static bool IsBefore(FamiliarityMatch candidate, FamiliarityMatch target) =>
        candidate.OccurredAt < target.OccurredAt
        || (candidate.OccurredAt == target.OccurredAt
            && string.CompareOrdinal(candidate.RiotMatchId, target.RiotMatchId) < 0);

    private static bool ContainsPlayer(FamiliarityMatch match, Guid playerId) =>
        match.Participants.Any(participant => participant.PlayerId == playerId);

    private static Guid[] EvaluatablePlayerIds(FamiliarityMatch match, Guid ownerPlayerId) =>
        match.Participants
            .Select(participant => participant.PlayerId)
            .Where(playerId => playerId.HasValue && playerId.Value != Guid.Empty && playerId.Value != ownerPlayerId)
            .Select(playerId => playerId!.Value)
            .Distinct()
            .ToArray();

    private static MatchFamiliarityResult EmptyResult(
        string targetRiotMatchId,
        MatchFamiliarityStatus status) =>
        new(targetRiotMatchId, 0, 0, 0, 0, status, []);
}
