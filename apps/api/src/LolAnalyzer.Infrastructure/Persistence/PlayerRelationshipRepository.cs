using System.Data;
using LolAnalyzer.Application.Analysis;
using LolAnalyzer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace LolAnalyzer.Infrastructure.Persistence;

public sealed class PlayerRelationshipRepository(LolAnalyzerDbContext dbContext) : IPlayerRelationshipRepository
{
    private const string RelationshipUpdateLockSql =
        "SELECT pg_advisory_xact_lock(hashtextextended('lol-analyzer:player-relationships', 0));";

    public async Task<IPlayerRelationshipUpdateScope> BeginIncrementalUpdateAsync(
        IReadOnlyCollection<string> changedRiotMatchIds,
        int batchSize,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(changedRiotMatchIds);
        ArgumentOutOfRangeException.ThrowIfLessThan(batchSize, 1);
        IDbContextTransaction? transaction = null;
        if (dbContext.Database.IsRelational())
        {
            // READ COMMITTED takes its data snapshots after the advisory lock is acquired.
            // A waiting updater therefore observes the previous updater's committed upserts.
            transaction = await dbContext.Database
                .BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken)
                .ConfigureAwait(false);
        }
        try
        {
            if (transaction is not null
                && dbContext.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true)
            {
                await dbContext.Database
                    .ExecuteSqlRawAsync(RelationshipUpdateLockSql, cancellationToken)
                    .ConfigureAwait(false);
            }
            var changedIds = changedRiotMatchIds
                .Where(matchId => !string.IsNullOrWhiteSpace(matchId))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            var changedMatches = await dbContext.Matches
                .AsNoTracking()
                .Where(match => changedIds.Contains(match.RiotMatchId))
                .Select(match => match.Id)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            if (changedMatches.Count == 0)
            {
                return new PlayerRelationshipUpdateScope(
                    dbContext,
                    transaction,
                    new IncrementalRelationshipAnalysisInput(0, [], []));
            }

            var changedParticipants = await dbContext.MatchParticipants
                .AsNoTracking()
                .Where(participant => changedMatches.Contains(participant.MatchId))
                .Select(participant => new ParticipantRow(
                    participant.MatchId,
                    participant.PlayerId,
                    participant.TeamId))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            var affectedPlayerIds = changedParticipants
                .Select(participant => participant.PlayerId)
                .ToHashSet();
            var affectedMatchIds = await dbContext.MatchParticipants
                .AsNoTracking()
                .Where(participant => affectedPlayerIds.Contains(participant.PlayerId))
                .Select(participant => participant.MatchId)
                .Distinct()
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            var affectedParticipants = await dbContext.MatchParticipants
                .AsNoTracking()
                .Where(participant => affectedMatchIds.Contains(participant.MatchId))
                .Select(participant => new ParticipantRow(
                    participant.MatchId,
                    participant.PlayerId,
                    participant.TeamId))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            var affectedPairs = BuildAffectedPairs(affectedParticipants, affectedPlayerIds);
            if (affectedPairs.Length == 0)
            {
                return new PlayerRelationshipUpdateScope(
                    dbContext,
                    transaction,
                    new IncrementalRelationshipAnalysisInput(changedMatches.Count, [], []));
            }

            var timelinePlayerIds = affectedPairs
                .SelectMany(pair => new[] { pair.PlayerAId, pair.PlayerBId })
                .Distinct()
                .ToArray();
            var snapshots = await LoadTimelineMatchesAsync(timelinePlayerIds, batchSize, cancellationToken)
                .ConfigureAwait(false);
            return new PlayerRelationshipUpdateScope(
                dbContext,
                transaction,
                new IncrementalRelationshipAnalysisInput(changedMatches.Count, affectedPairs, snapshots));
        }
        catch
        {
            if (transaction is not null)
            {
                await transaction.DisposeAsync().ConfigureAwait(false);
            }
            throw;
        }
    }

    private async Task<IReadOnlyList<RelationshipMatchSnapshot>> LoadTimelineMatchesAsync(
        IReadOnlyCollection<Guid> playerIds,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var snapshots = new List<RelationshipMatchSnapshot>();
        for (var offset = 0; ; offset += batchSize)
        {
            var matches = await dbContext.Matches
                .AsNoTracking()
                .Where(match => match.Participants.Any(participant => playerIds.Contains(participant.PlayerId)))
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

        return snapshots;
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

    private static PlayerRelationshipPair[] BuildAffectedPairs(
        IReadOnlyCollection<ParticipantRow> participants,
        HashSet<Guid> affectedPlayerIds)
    {
        var pairs = new HashSet<PlayerRelationshipPair>();
        foreach (var matchParticipants in participants.GroupBy(participant => participant.MatchId))
        {
            var playerIds = matchParticipants
                .Select(participant => participant.PlayerId)
                .Distinct()
                .Order()
                .ToArray();
            for (var first = 0; first < playerIds.Length; first++)
            {
                for (var second = first + 1; second < playerIds.Length; second++)
                {
                    if (affectedPlayerIds.Contains(playerIds[first])
                        || affectedPlayerIds.Contains(playerIds[second]))
                    {
                        pairs.Add(new PlayerRelationshipPair(playerIds[first], playerIds[second]));
                    }
                }
            }
        }

        return pairs
            .OrderBy(pair => pair.PlayerAId)
            .ThenBy(pair => pair.PlayerBId)
            .ToArray();
    }

    private sealed class PlayerRelationshipUpdateScope(
        LolAnalyzerDbContext context,
        IDbContextTransaction? transaction,
        IncrementalRelationshipAnalysisInput input) : IPlayerRelationshipUpdateScope
    {
        private bool committed;

        public IncrementalRelationshipAnalysisInput Input { get; } = input;

        public async Task UpsertRelationshipsAsync(
            IReadOnlyCollection<PlayerRelationshipAggregate> relationships,
            int batchSize,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(relationships);
            ArgumentOutOfRangeException.ThrowIfLessThan(batchSize, 1);
            var affectedPairs = Input.AffectedPairs.ToHashSet();
            var aggregatesByPair = relationships.ToDictionary(
                relationship => new PlayerRelationshipPair(
                    relationship.PlayerAId,
                    relationship.PlayerBId));
            if (aggregatesByPair.Count != affectedPairs.Count
                || aggregatesByPair.Keys.Any(pair => !affectedPairs.Contains(pair)))
            {
                throw new InvalidOperationException(
                    "Incremental relationship analysis must produce exactly the affected canonical pairs.");
            }

            var endpointIds = affectedPairs
                .SelectMany(pair => new[] { pair.PlayerAId, pair.PlayerBId })
                .Distinct()
                .ToArray();
            var existingRelationships = await context.PlayerRelationships
                .Where(relationship => endpointIds.Contains(relationship.PlayerAId)
                    || endpointIds.Contains(relationship.PlayerBId))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            var existingByPair = existingRelationships
                .Where(relationship => affectedPairs.Contains(new PlayerRelationshipPair(
                    relationship.PlayerAId,
                    relationship.PlayerBId)))
                .ToDictionary(relationship => new PlayerRelationshipPair(
                    relationship.PlayerAId,
                    relationship.PlayerBId));

            foreach (var batch in aggregatesByPair.Chunk(batchSize))
            {
                foreach (var (pair, aggregate) in batch)
                {
                    if (!existingByPair.TryGetValue(pair, out var entity))
                    {
                        entity = new PlayerRelationship
                        {
                            PlayerAId = pair.PlayerAId,
                            PlayerBId = pair.PlayerBId,
                        };
                        context.PlayerRelationships.Add(entity);
                    }

                    Apply(aggregate, entity);
                }

                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task CommitAsync(CancellationToken cancellationToken)
        {
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
            committed = true;
        }

        public async ValueTask DisposeAsync()
        {
            if (transaction is not null && !committed)
            {
                await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            }

            if (transaction is not null)
            {
                await transaction.DisposeAsync().ConfigureAwait(false);
            }
        }

        private static void Apply(PlayerRelationshipAggregate aggregate, PlayerRelationship entity)
        {
            entity.MatchesTogether = aggregate.MatchesTogether;
            entity.SameTeamMatches = aggregate.SameTeamMatches;
            entity.OppositeTeamMatches = aggregate.OppositeTeamMatches;
            entity.RecentMatchesTogether = aggregate.RecentMatchesTogether;
            entity.ConsecutiveMatches = aggregate.ConsecutiveMatches;
            entity.FirstSeenAt = aggregate.FirstSeenAt;
            entity.LastSeenAt = aggregate.LastSeenAt;
            entity.RelationshipScore = aggregate.RelationshipScore;
            entity.RelationshipConfidence = aggregate.RelationshipConfidence;
        }
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
