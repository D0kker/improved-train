using LolAnalyzer.Application.Analysis;

namespace LolAnalyzer.Application.Stateless;

public sealed record PlayerIdentity(string Puuid, string GameName, string TagLine, string PlatformRegion);

public sealed record AnalysisFailure(string Stage, string Code, string? Puuid = null, string? MatchId = null);

public sealed record ParticipantCoverage(string Puuid, int Requested, int MatchIds, int EligibleMatches, bool Limited);

public sealed record AnalysisCoverage(
    string Status,
    int Participants,
    int MatchIdsObtained,
    int UniqueMatchIds,
    int MatchesRecovered,
    IReadOnlyList<ParticipantCoverage> Players,
    IReadOnlyList<AnalysisFailure> Failures);

public sealed record PlayerSearchResult(
    PlayerIdentity Player,
    PlayerSummary Summary,
    PagedPlayerMatches Matches,
    IReadOnlyList<PlayerEncounterView> Encounters,
    AnalysisCoverage Coverage);

public sealed record HistoricalPairView(
    string FirstPuuid,
    string SecondPuuid,
    int MatchesTogether,
    int AllyMatches,
    int EnemyMatches,
    DateTimeOffset FirstSeenAt,
    DateTimeOffset LastSeenAt,
    int Score,
    string Confidence);

public sealed record MatchAnalysisResult(
    MatchDetail Match,
    PlayerIdentity Owner,
    PlayerSummary Summary,
    IReadOnlyList<PlayerEncounterView> Encounters,
    IReadOnlyList<HistoricalPairView> Relationships,
    AnalysisCoverage Coverage);

public sealed class OnDemandAnalysisOptions
{
    public const string SectionName = "OnDemandAnalysis";

    public int RequestTimeoutSeconds { get; init; } = 180;

    public int RequestConcurrency { get; init; } = 3;

    public void Validate()
    {
        if (RequestTimeoutSeconds is < 5 or > 300 || RequestConcurrency is < 1 or > 5)
            throw new InvalidOperationException("Analysis timeout must be 5-300 seconds and concurrency 1-5.");
    }
}
