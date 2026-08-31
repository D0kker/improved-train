namespace LolAnalyzer.Application.Analysis;

public sealed record EncounterParticipant(Guid PlayerId, int TeamId);

public sealed record EncounterMatch(
    Guid MatchId,
    DateTimeOffset OccurredAt,
    Guid OwnerPlayerId,
    int OwnerTeamId,
    bool OwnerWon,
    IReadOnlyList<EncounterParticipant> Participants);

public sealed record PlayerEncounterAggregate(
    Guid OtherPlayerId,
    int TotalMatches,
    int SameTeamMatches,
    int EnemyTeamMatches,
    int WinsTogether,
    int LossesTogether,
    int WinsAgainst,
    int LossesAgainst,
    DateTimeOffset FirstSeenAt,
    DateTimeOffset LastSeenAt);

public sealed record PlayerAnalysisInput(
    Guid OwnerPlayerId,
    IReadOnlyList<EncounterMatch> Matches);

public sealed record PlayerAnalysisResult(
    Guid OwnerPlayerId,
    int MatchesAnalyzed,
    int EncountersPersisted);

public sealed record FamiliarityParticipant(Guid? PlayerId);

public sealed record FamiliarityMatch(
    string RiotMatchId,
    DateTimeOffset OccurredAt,
    IReadOnlyList<FamiliarityParticipant> Participants);

public sealed record MatchFamiliarityInput(
    Guid OwnerPlayerId,
    string TargetRiotMatchId,
    IReadOnlyList<FamiliarityMatch> Matches);

public enum MatchFamiliarityStatus
{
    Available,
    NoPriorHistory,
    NoEvaluableParticipants,
    OwnerNotPresent,
}

public sealed record MatchFamiliarityResult(
    string TargetRiotMatchId,
    int KnownPlayers,
    int UnknownPlayers,
    int EvaluablePlayers,
    decimal FamiliarityPercentage,
    MatchFamiliarityStatus Status,
    IReadOnlyList<Guid> KnownPlayerIds);

public sealed record PlayerSummary(
    string Puuid,
    string GameName,
    string TagLine,
    int MatchesAnalyzed,
    int Wins,
    int Losses,
    double WinRate,
    int UniquePlayersEncountered,
    int RepeatedPlayers,
    DateTimeOffset DataUpdatedAt);

public sealed record PlayerEncounterView(
    string OtherPlayerPuuid,
    string GameName,
    string TagLine,
    int TotalMatches,
    int SameTeamMatches,
    int EnemyTeamMatches,
    int WinsTogether,
    int LossesTogether,
    int WinsAgainst,
    int LossesAgainst,
    DateTimeOffset FirstSeenAt,
    DateTimeOffset LastSeenAt);

public sealed record PlayerMatchListItem(
    string RiotMatchId,
    int? QueueId,
    DateTimeOffset? GameStartTimestamp,
    int? GameDurationSeconds,
    string ChampionName,
    int Kills,
    int Deaths,
    int Assists,
    bool Win);

public sealed record PagedPlayerMatches(
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyList<PlayerMatchListItem> Items);

public sealed record MatchParticipantDetail(
    string Puuid,
    string GameName,
    string TagLine,
    int TeamId,
    int ParticipantId,
    string ChampionName,
    string? TeamPosition,
    int Kills,
    int Deaths,
    int Assists,
    bool Win);

public sealed record MatchTeamDetail(
    int TeamId,
    IReadOnlyList<MatchParticipantDetail> Participants);

public sealed record MatchPremadeGroupMember(
    string Puuid,
    string GameName,
    string TagLine);

public sealed record MatchPremadeGroup(
    int GroupNumber,
    int TeamId,
    string Classification,
    string Label,
    IReadOnlyList<MatchPremadeGroupMember> Members);

public sealed record MatchFamiliarityView(
    int KnownPlayers,
    int UnknownPlayers,
    int EvaluablePlayers,
    decimal FamiliarityPercentage,
    string Status);

public sealed record MatchDetail(
    string RiotMatchId,
    int? QueueId,
    DateTimeOffset? GameStartTimestamp,
    int? GameDurationSeconds,
    IReadOnlyList<MatchTeamDetail> Teams)
{
    public IReadOnlyList<MatchPremadeGroup> PremadeGroups { get; init; } = [];

    public MatchFamiliarityView? Familiarity { get; init; }
}
