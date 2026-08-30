namespace LolAnalyzer.Application.Analysis;

public sealed class RelationshipScoreCalculator
{
    private readonly RelationshipScoreOptions _options;

    public RelationshipScoreCalculator(RelationshipScoreOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        _options = options;
    }

    public RelationshipScoreResult Calculate(RelationshipScoreInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.Matches);
        ValidateEvidence(input);

        var chronologicalMatches = input.Matches
            .OrderBy(match => match.OccurredAt)
            .ThenBy(match => match.MatchId, StringComparer.Ordinal)
            .ToArray();
        var encounters = chronologicalMatches.Where(match => match.Encountered).ToArray();
        var recentMatches = chronologicalMatches
            .Where(match => input.EvaluatedAt - match.OccurredAt <= _options.RecencyWindow)
            .OrderByDescending(match => match.OccurredAt)
            .ThenBy(match => match.MatchId, StringComparer.Ordinal)
            .Take(_options.RecentMatchWindow)
            .OrderBy(match => match.OccurredAt)
            .ThenBy(match => match.MatchId, StringComparer.Ordinal)
            .ToArray();

        var recentEncounters = recentMatches.Count(match => match.Encountered);
        var longestConsecutiveRun = LongestConsecutiveRun(recentMatches);
        var sameTeamEncounters = encounters.Count(match => match.SameTeam);

        var factors = new RelationshipScoreFactors(
            CreateFactor(
                encounters.Length,
                _options.MatchesTogetherForFullScore,
                _options.MatchesTogetherWeight),
            CreateFactor(
                recentEncounters,
                recentMatches.Length,
                _options.RecentFrequencyWeight),
            CreateFactor(
                longestConsecutiveRun,
                _options.ConsecutiveMatchesForFullScore,
                _options.ConsecutiveMatchesWeight),
            CreateFactor(
                sameTeamEncounters,
                encounters.Length,
                _options.SameTeamWeight));

        var unboundedScore = factors.MatchesTogether.WeightedScore
            + factors.RecentFrequency.WeightedScore
            + factors.ConsecutiveMatches.WeightedScore
            + factors.SameTeam.WeightedScore;
        var score = Math.Clamp(
            decimal.ToInt32(decimal.Round(unboundedScore, 0, MidpointRounding.AwayFromZero)),
            0,
            100);

        return new RelationshipScoreResult(score, Classify(score), factors);
    }

    public RelationshipConfidence Classify(int score)
    {
        if (score is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(score), score, "Relationship score must be between 0 and 100.");
        }

        if (score >= _options.VeryHighThreshold)
        {
            return RelationshipConfidence.VeryHigh;
        }

        if (score >= _options.HighThreshold)
        {
            return RelationshipConfidence.High;
        }

        return score >= _options.MediumThreshold
            ? RelationshipConfidence.Medium
            : RelationshipConfidence.Low;
    }

    private static RelationshipScoreFactor CreateFactor(int count, int total, int weight)
    {
        var normalizedValue = total == 0
            ? 0m
            : Math.Clamp((decimal)count / total, 0m, 1m);

        return new RelationshipScoreFactor(
            count,
            total,
            weight,
            normalizedValue,
            normalizedValue * weight);
    }

    private static int LongestConsecutiveRun(IReadOnlyList<RelationshipMatchEvidence> matches)
    {
        var longest = 0;
        var current = 0;
        foreach (var match in matches)
        {
            current = match.Encountered ? current + 1 : 0;
            longest = Math.Max(longest, current);
        }

        return longest;
    }

    private static void ValidateEvidence(RelationshipScoreInput input)
    {
        var matchIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var match in input.Matches)
        {
            if (string.IsNullOrWhiteSpace(match.MatchId))
            {
                throw new ArgumentException("Every relationship match evidence item requires a match ID.", nameof(input));
            }

            if (!matchIds.Add(match.MatchId))
            {
                throw new ArgumentException("Relationship match evidence cannot contain duplicate match IDs.", nameof(input));
            }

            if (match.OccurredAt > input.EvaluatedAt)
            {
                throw new ArgumentException("Relationship match evidence cannot occur after the evaluation time.", nameof(input));
            }

            if (match.SameTeam && !match.Encountered)
            {
                throw new ArgumentException("A player cannot be on the same team in a match where they were not encountered.", nameof(input));
            }
        }
    }
}
