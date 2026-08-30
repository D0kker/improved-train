namespace LolAnalyzer.Application.Analysis;

public sealed record RelationshipParticipant(Guid PlayerId, int TeamId);

public sealed record RelationshipMatchSnapshot(
    string MatchId,
    DateTimeOffset OccurredAt,
    IReadOnlyList<RelationshipParticipant> Participants);

public sealed record PlayerRelationshipAggregate(
    Guid PlayerAId,
    Guid PlayerBId,
    int MatchesTogether,
    int SameTeamMatches,
    int OppositeTeamMatches,
    int RecentMatchesTogether,
    int ConsecutiveMatches,
    DateTimeOffset FirstSeenAt,
    DateTimeOffset LastSeenAt,
    int RelationshipScore,
    string RelationshipConfidence);

public sealed record PlayerRelationshipAnalysisResult(int MatchesAnalyzed, int RelationshipsRebuilt);
