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

    public async Task<PagedPlayerRelationshipQuery?> GetRelationshipsAsync(
        string puuid,
        int page,
        int pageSize,
        RelationshipConfidence minimumConfidence,
        int minimumScore,
        CancellationToken cancellationToken)
    {
        var player = await dbContext.Players
            .AsNoTracking()
            .Where(player => player.Puuid == puuid)
            .Select(player => new { player.Id, player.Puuid, player.GameName, player.TagLine })
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        if (player is null)
        {
            return null;
        }

        var confidenceLabels = Enum.GetValues<RelationshipConfidence>()
            .Where(confidence => confidence >= minimumConfidence)
            .Select(confidence => confidence.ToLabel())
            .ToArray();
        var query = dbContext.PlayerRelationships
            .AsNoTracking()
            .Where(relationship =>
                (relationship.PlayerAId == player.Id || relationship.PlayerBId == player.Id)
                && confidenceLabels.Contains(relationship.RelationshipConfidence)
                && relationship.RelationshipScore >= minimumScore);
        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var rows = await query
            .OrderByDescending(relationship => relationship.RelationshipScore)
            .ThenByDescending(relationship => relationship.MatchesTogether)
            .ThenBy(relationship => relationship.PlayerAId == player.Id
                ? relationship.PlayerB.Puuid
                : relationship.PlayerA.Puuid)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(relationship => new RelationshipQueryRow(
                relationship.PlayerAId == player.Id ? relationship.PlayerB.Puuid : relationship.PlayerA.Puuid,
                relationship.PlayerAId == player.Id ? relationship.PlayerB.GameName : relationship.PlayerA.GameName,
                relationship.PlayerAId == player.Id ? relationship.PlayerB.TagLine : relationship.PlayerA.TagLine,
                relationship.MatchesTogether,
                relationship.SameTeamMatches,
                relationship.OppositeTeamMatches,
                relationship.RecentMatchesTogether,
                relationship.ConsecutiveMatches,
                relationship.FirstSeenAt,
                relationship.LastSeenAt,
                relationship.RelationshipScore,
                relationship.RelationshipConfidence))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedPlayerRelationshipQuery(
            player.Puuid,
            player.GameName,
            player.TagLine,
            page,
            pageSize,
            totalCount,
            rows.Select(ToQueryItem).ToArray());
    }

    public async Task<MatchPremadeGroupInput?> LoadMatchPremadeGroupInputAsync(
        string riotMatchId,
        CancellationToken cancellationToken)
    {
        var participants = await dbContext.MatchParticipants
            .AsNoTracking()
            .Where(participant => participant.Match.RiotMatchId == riotMatchId)
            .OrderBy(participant => participant.TeamId)
            .ThenBy(participant => participant.ParticipantId)
            .Select(participant => new MatchPremadeParticipant(
                participant.PlayerId,
                participant.Player.Puuid,
                participant.Player.GameName,
                participant.Player.TagLine,
                participant.TeamId))
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
        if (participants.Length == 0)
        {
            var matchExists = await dbContext.Matches
                .AsNoTracking()
                .AnyAsync(match => match.RiotMatchId == riotMatchId, cancellationToken)
                .ConfigureAwait(false);
            return matchExists ? new MatchPremadeGroupInput([], []) : null;
        }

        var playerIds = participants.Select(participant => participant.PlayerId).ToArray();
        var relationships = await dbContext.PlayerRelationships
            .AsNoTracking()
            .Where(relationship =>
                playerIds.Contains(relationship.PlayerAId)
                && playerIds.Contains(relationship.PlayerBId))
            .OrderBy(relationship => relationship.PlayerAId)
            .ThenBy(relationship => relationship.PlayerBId)
            .Select(relationship => new
            {
                relationship.PlayerAId,
                relationship.PlayerBId,
                relationship.MatchesTogether,
                relationship.SameTeamMatches,
                relationship.RelationshipConfidence,
            })
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        return new MatchPremadeGroupInput(
            participants,
            relationships.Select(relationship => new MatchPremadeRelationship(
                relationship.PlayerAId,
                relationship.PlayerBId,
                relationship.MatchesTogether,
                relationship.SameTeamMatches,
                ParseConfidence(relationship.RelationshipConfidence))).ToArray());
    }

    private static RelationshipConfidence ParseConfidence(string label)
    {
        if (!RelationshipConfidenceExtensions.TryParseLabel(label, out var confidence))
        {
            throw new InvalidDataException("A persisted relationship contains an unsupported confidence label.");
        }

        return confidence;
    }

    private static PlayerRelationshipQueryItem ToQueryItem(RelationshipQueryRow row)
    {
        var confidence = ParseConfidence(row.RelationshipConfidence);

        return new PlayerRelationshipQueryItem(
            row.OtherPlayerPuuid,
            row.GameName,
            row.TagLine,
            row.MatchesTogether,
            row.SameTeamMatches,
            row.OppositeTeamMatches,
            row.RecentMatchesTogether,
            row.ConsecutiveMatches,
            row.FirstSeenAt,
            row.LastSeenAt,
            row.RelationshipScore,
            confidence);
    }

    private sealed record MatchRow(Guid Id, string RiotMatchId, DateTimeOffset OccurredAt);

    private sealed record ParticipantRow(Guid MatchId, Guid PlayerId, int TeamId);

    private sealed record RelationshipQueryRow(
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
        string RelationshipConfidence);
}
