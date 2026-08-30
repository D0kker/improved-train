namespace LolAnalyzer.Application.Matches;

public sealed record MatchSyncResult(
    int RequestedCount,
    int MatchIdsReturned,
    int AlreadyStored,
    int Downloaded,
    int Persisted,
    int NotFound);
