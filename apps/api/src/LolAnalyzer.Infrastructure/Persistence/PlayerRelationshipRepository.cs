using System.Data;
using LolAnalyzer.Application.Analysis;
using LolAnalyzer.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LolAnalyzer.Infrastructure.Persistence;

public sealed class PlayerRelationshipRepository(LolAnalyzerDbContext dbContext) : IPlayerRelationshipRepository
{
    public async Task<IReadOnlyList<RelationshipMatchSnapshot>> LoadMatchesAsync(
        int batchSize,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(batchSize, 1);
        var snapshots = new List<RelationshipMatchSnapshot>();
        await using var transaction = await dbContext.Database
            .BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken)
            .ConfigureAwait(false);

        for (var offset = 0; ; offset += batchSize)
        {
            var matches = await dbContext.Matches
                .AsNoTracking()
                .OrderBy(match => match.RiotMatchId)
                .Skip(offset)
                .Take(batchSize)
                .Select(match => new MatchRow(
                    match.Id,
                    match.RiotMatchId,
                    match.GameEndTimestamp
                        ?? match.GameStartTimestamp
                        ?? match.GameCreation
                        ?? match.CreatedAt))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            if (matches.Count == 0)
            {
                break;
            }

            var matchIds = matches.Select(match => match.Id).ToArray();
            var participants = await dbContext.MatchParticipants
                .AsNoTracking()
                .Where(participant => matchIds.Contains(participant.MatchId))
                .Select(participant => new ParticipantRow(
                    participant.MatchId,
                    participant.PlayerId,
                    participant.TeamId))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            var participantsByMatch = participants.ToLookup(participant => participant.MatchId);

            snapshots.AddRange(matches.Select(match => new RelationshipMatchSnapshot(
                match.RiotMatchId,
                match.OccurredAt,
                participantsByMatch[match.Id]
                    .Select(participant => new RelationshipParticipant(participant.PlayerId, participant.TeamId))
                    .ToArray())));

            if (matches.Count < batchSize)
            {
                break;
            }
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return snapshots;
    }

    public async Task ReplaceRelationshipsAsync(
        IReadOnlyCollection<PlayerRelationshipAggregate> relationships,
        int batchSize,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(batchSize, 1);
        await using var transaction = await dbContext.Database
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);
        await dbContext.PlayerRelationships.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);

        foreach (var batch in relationships.Chunk(batchSize))
        {
            dbContext.PlayerRelationships.AddRange(batch.Select(relationship => new PlayerRelationship
            {
                PlayerAId = relationship.PlayerAId,
                PlayerBId = relationship.PlayerBId,
                MatchesTogether = relationship.MatchesTogether,
                SameTeamMatches = relationship.SameTeamMatches,
                OppositeTeamMatches = relationship.OppositeTeamMatches,
                RecentMatchesTogether = relationship.RecentMatchesTogether,
                ConsecutiveMatches = relationship.ConsecutiveMatches,
                FirstSeenAt = relationship.FirstSeenAt,
                LastSeenAt = relationship.LastSeenAt,
                RelationshipScore = relationship.RelationshipScore,
                RelationshipConfidence = relationship.RelationshipConfidence,
            }));
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            dbContext.ChangeTracker.Clear();
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    private sealed record MatchRow(Guid Id, string RiotMatchId, DateTimeOffset OccurredAt);

    private sealed record ParticipantRow(Guid MatchId, Guid PlayerId, int TeamId);
}
