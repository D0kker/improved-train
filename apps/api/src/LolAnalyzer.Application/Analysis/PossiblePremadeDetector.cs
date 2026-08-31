namespace LolAnalyzer.Application.Analysis;

public sealed class PossiblePremadeDetector
{
    private readonly PremadeDetectionOptions _options;

    public PossiblePremadeDetector(PremadeDetectionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        _options = options;
    }

    public PremadeDetectionResult Detect(PremadeDetectionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ValidateInput(input);

        var sameTeamRatio = input.MatchesTogether == 0
            ? 0m
            : (decimal)input.SameTeamMatches / input.MatchesTogether;
        var classification = MeetsThresholds(
            input,
            sameTeamRatio,
            _options.LikelyMinimumMatchesTogether,
            _options.LikelyMinimumSameTeamRatio,
            _options.LikelyMinimumConfidence)
                ? PremadeClassification.LikelyPremade
                : MeetsThresholds(
                    input,
                    sameTeamRatio,
                    _options.PossibleMinimumMatchesTogether,
                    _options.PossibleMinimumSameTeamRatio,
                    _options.PossibleMinimumConfidence)
                    ? PremadeClassification.PossiblePremade
                    : PremadeClassification.NoEvidence;

        return new PremadeDetectionResult(
            classification,
            input.MatchesTogether,
            input.SameTeamMatches,
            sameTeamRatio,
            input.RelationshipConfidence);
    }

    private static bool MeetsThresholds(
        PremadeDetectionInput input,
        decimal sameTeamRatio,
        int minimumMatchesTogether,
        decimal minimumSameTeamRatio,
        RelationshipConfidence minimumConfidence) =>
        input.MatchesTogether >= minimumMatchesTogether
        && sameTeamRatio >= minimumSameTeamRatio
        && input.RelationshipConfidence >= minimumConfidence;

    private static void ValidateInput(PremadeDetectionInput input)
    {
        if (input.MatchesTogether < 0
            || input.SameTeamMatches < 0
            || input.SameTeamMatches > input.MatchesTogether)
        {
            throw new ArgumentException(
                "Premade evidence counts must be nonnegative and same-team matches cannot exceed total matches.",
                nameof(input));
        }

        if (!Enum.IsDefined(input.RelationshipConfidence))
        {
            throw new ArgumentOutOfRangeException(
                nameof(input),
                input.RelationshipConfidence,
                "The relationship confidence level is not defined.");
        }
    }
}
