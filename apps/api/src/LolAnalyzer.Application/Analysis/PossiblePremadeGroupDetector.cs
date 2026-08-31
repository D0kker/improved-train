namespace LolAnalyzer.Application.Analysis;

public sealed record PremadeGroupPair(
    Guid PlayerAId,
    Guid PlayerBId,
    PremadeClassification Classification);

public sealed record PossiblePremadeGroup(
    IReadOnlyList<Guid> PlayerIds,
    PremadeClassification Classification)
{
    public string Label => Classification == PremadeClassification.LikelyPremade
        ? "likely premade group"
        : "possible premade group";
}

public sealed class PremadeGroupDetectionOptions
{
    public const string SectionName = "PremadeGroupDetection";

    public int MinimumGroupSize { get; init; } = 3;

    public int MaximumGroupSize { get; init; } = 5;

    public int MaximumCandidates { get; init; } = 30;

    public int MaximumCombinations { get; init; } = 100_000;

    public void Validate()
    {
        if (MinimumGroupSize < 3 || MaximumGroupSize > 5 || MinimumGroupSize > MaximumGroupSize)
        {
            throw new ArgumentException("Group sizes must define a valid range between 3 and 5 players.");
        }

        if (MaximumCandidates < MaximumGroupSize || MaximumCombinations <= 0)
        {
            throw new ArgumentException("Candidate and combination limits must accommodate the configured group size.");
        }
    }
}

public sealed class PossiblePremadeGroupDetector
{
    private readonly PremadeGroupDetectionOptions _options;

    public PossiblePremadeGroupDetector(PremadeGroupDetectionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        _options = options;
    }

    public IReadOnlyList<PossiblePremadeGroup> Detect(IReadOnlyCollection<PremadeGroupPair> pairs)
    {
        ArgumentNullException.ThrowIfNull(pairs);
        var pairMap = ValidateAndIndex(pairs);
        var candidates = pairs
            .SelectMany(pair => new[] { pair.PlayerAId, pair.PlayerBId })
            .Distinct()
            .Order()
            .ToArray();
        if (candidates.Length > _options.MaximumCandidates)
        {
            throw new ArgumentException("Premade group candidate limit exceeded.", nameof(pairs));
        }

        var validGroups = new List<PossiblePremadeGroup>();
        var combinationsEvaluated = 0;
        for (var size = _options.MinimumGroupSize; size <= Math.Min(_options.MaximumGroupSize, candidates.Length); size++)
        {
            foreach (var playerIds in Combinations(candidates, size))
            {
                combinationsEvaluated++;
                if (combinationsEvaluated > _options.MaximumCombinations)
                {
                    throw new ArgumentException("Premade group combination limit exceeded.", nameof(pairs));
                }

                var classification = ClassifyClique(playerIds, pairMap);
                if (classification is not PremadeClassification.NoEvidence)
                {
                    validGroups.Add(new PossiblePremadeGroup(playerIds, classification));
                }
            }
        }

        return validGroups
            .Where(group => !validGroups.Any(candidate =>
                candidate.PlayerIds.Count > group.PlayerIds.Count
                && group.PlayerIds.All(candidate.PlayerIds.Contains)))
            .OrderByDescending(group => group.PlayerIds.Count)
            .ThenBy(group => string.Join(':', group.PlayerIds))
            .ToArray();
    }

    private static Dictionary<(Guid PlayerAId, Guid PlayerBId), PremadeClassification> ValidateAndIndex(
        IReadOnlyCollection<PremadeGroupPair> pairs)
    {
        var result = new Dictionary<(Guid, Guid), PremadeClassification>();
        foreach (var pair in pairs)
        {
            if (pair.PlayerAId == Guid.Empty || pair.PlayerBId == Guid.Empty || pair.PlayerAId == pair.PlayerBId)
            {
                throw new ArgumentException("Premade group pairs require two distinct, identifiable players.", nameof(pairs));
            }

            if (!Enum.IsDefined(pair.Classification))
            {
                throw new ArgumentOutOfRangeException(nameof(pairs), pair.Classification, "Unknown classification.");
            }

            var key = CanonicalPair(pair.PlayerAId, pair.PlayerBId);
            if (!result.TryAdd(key, pair.Classification))
            {
                throw new ArgumentException("Premade group pairs must be unique.", nameof(pairs));
            }
        }

        return result;
    }

    private static PremadeClassification ClassifyClique(
        Guid[] playerIds,
        Dictionary<(Guid PlayerAId, Guid PlayerBId), PremadeClassification> pairs)
    {
        var classification = PremadeClassification.LikelyPremade;
        for (var first = 0; first < playerIds.Length; first++)
        {
            for (var second = first + 1; second < playerIds.Length; second++)
            {
                if (!pairs.TryGetValue(CanonicalPair(playerIds[first], playerIds[second]), out var pairClassification)
                    || pairClassification == PremadeClassification.NoEvidence)
                {
                    return PremadeClassification.NoEvidence;
                }

                if (pairClassification == PremadeClassification.PossiblePremade)
                {
                    classification = PremadeClassification.PossiblePremade;
                }
            }
        }

        return classification;
    }

    private static IEnumerable<Guid[]> Combinations(Guid[] candidates, int size)
    {
        var selection = new Guid[size];
        return Enumerate(start: 0, depth: 0);

        IEnumerable<Guid[]> Enumerate(int start, int depth)
        {
            if (depth == size)
            {
                yield return selection.ToArray();
                yield break;
            }

            for (var index = start; index <= candidates.Length - (size - depth); index++)
            {
                selection[depth] = candidates[index];
                foreach (var combination in Enumerate(index + 1, depth + 1))
                {
                    yield return combination;
                }
            }
        }
    }

    private static (Guid, Guid) CanonicalPair(Guid first, Guid second) =>
        first.CompareTo(second) < 0 ? (first, second) : (second, first);
}
