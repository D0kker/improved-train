namespace LolAnalyzer.Application.Riot;

public sealed record RiotMatchData(
    string RiotMatchId,
    int? QueueId,
    DateTimeOffset? GameCreation,
    DateTimeOffset? GameStartTimestamp,
    DateTimeOffset GameEndTimestamp,
    int? GameDurationSeconds,
    string? GameVersion,
    string RawJson,
    IReadOnlyList<RiotMatchParticipantData> Participants);

public sealed record RiotMatchParticipantData(
    string Puuid,
    string? GameName,
    string? TagLine,
    int TeamId,
    int ParticipantId,
    int ChampionId,
    string ChampionName,
    string? TeamPosition,
    string? IndividualPosition,
    int Kills,
    int Deaths,
    int Assists,
    bool Win,
    int GoldEarned,
    int TotalDamageDealtToChampions,
    int VisionScore,
    int Cs);
