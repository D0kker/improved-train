namespace LolAnalyzer.Application.Analysis;

public sealed record PlayerRelationshipQueryItem(
    string OtherPlayerPuuid,
    string GameName,
    string TagLine,
    int MatchesTogether,
    int SameTeamMatches,
    int OppositeTeamMatches,
    int RecentMatchesTogether,
    int ConsecutiveMatches,
    DateTimeOffset FirstSeenAt,
    DateTimeOffset LastSeenAt,
    int RelationshipScore,
    RelationshipConfidence RelationshipConfidence);

public sealed record PagedPlayerRelationshipQuery(
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyList<PlayerRelationshipQueryItem> Items);

public sealed record PlayerRelationshipView(
    string OtherPlayerPuuid,
    string GameName,
    string TagLine,
    int MatchesTogether,
    int SameTeamMatches,
    int OppositeTeamMatches,
    decimal SameTeamRatio,
    int RecentMatchesTogether,
    int ConsecutiveMatches,
    DateTimeOffset FirstSeenAt,
    DateTimeOffset LastSeenAt,
    int RelationshipScore,
    string RelationshipConfidence,
    string? PremadeLabel);

public sealed record PagedPlayerRelationships(
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyList<PlayerRelationshipView> Items);
