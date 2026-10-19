namespace LolAnalyzer.Application.Analysis;

public sealed record RelationshipParticipant(Guid PlayerId, int TeamId);

public sealed record RelationshipMatchSnapshot(
    string MatchId,
    DateTimeOffset OccurredAt,
    IReadOnlyList<RelationshipParticipant> Participants);

public sealed record PlayerRelationshipPair(Guid PlayerAId, Guid PlayerBId)
{
    public static PlayerRelationshipPair Create(Guid firstPlayerId, Guid secondPlayerId)
    {
        if (firstPlayerId == secondPlayerId)
        {
            throw new ArgumentException("A relationship requires two different players.");
        }

        return firstPlayerId.CompareTo(secondPlayerId) < 0
            ? new PlayerRelationshipPair(firstPlayerId, secondPlayerId)
            : new PlayerRelationshipPair(secondPlayerId, firstPlayerId);
    }
}

public sealed record IncrementalRelationshipAnalysisInput(
    int ChangedMatches,
    IReadOnlyList<PlayerRelationshipPair> AffectedPairs,
    IReadOnlyList<RelationshipMatchSnapshot> Matches);

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
