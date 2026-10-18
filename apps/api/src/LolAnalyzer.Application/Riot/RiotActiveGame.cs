namespace LolAnalyzer.Application.Riot;

public sealed record RiotActiveGame(
    long GameId,
    int QueueId,
    DateTimeOffset GameStartTime,
    int GameLengthSeconds);
