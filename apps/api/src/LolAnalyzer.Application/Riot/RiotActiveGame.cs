namespace LolAnalyzer.Application.Riot;

public sealed record RiotActiveGame(
    long GameId,
    int QueueId,
    DateTimeOffset GameStartTime,
    int GameLengthSeconds,
    IReadOnlyList<RiotActiveGameParticipant> Participants);

public sealed record RiotActiveGameParticipant(
    string? Puuid,
    string? GameName,
    string? TagLine,
    string? SummonerId,
    int ChampionId,
    int TeamId);
