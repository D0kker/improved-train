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
    string PlayerPuuid,
    string PlayerGameName,
    string PlayerTagLine,
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
    string PlayerPuuid,
    string PlayerGameName,
    string PlayerTagLine,
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyList<PlayerRelationshipView> Items);

public sealed record PlayerNetworkNode(
    string Puuid,
    string GameName,
    string TagLine,
    bool IsCenter);

public sealed record PlayerNetworkEdge(
    string SourcePuuid,
    string TargetPuuid,
    int MatchesTogether,
    int SameTeamMatches,
    int OppositeTeamMatches,
    decimal SameTeamRatio,
    int RelationshipScore,
    string RelationshipConfidence,
    string? PremadeLabel);

public sealed record PlayerNetworkMetadata(
    int Depth,
    bool Truncated,
    int TotalAvailableNodes,
    int TotalAvailableEdges,
    int AppliedMaxNodes,
    int AppliedMaxEdges);

public sealed record PlayerNetwork(
    PlayerNetworkNode Center,
    IReadOnlyList<PlayerNetworkNode> Nodes,
    IReadOnlyList<PlayerNetworkEdge> Edges,
    PlayerNetworkMetadata Metadata);
