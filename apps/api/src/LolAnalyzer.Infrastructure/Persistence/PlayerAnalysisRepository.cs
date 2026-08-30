using LolAnalyzer.Application.Analysis;
using LolAnalyzer.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LolAnalyzer.Infrastructure.Persistence;

public sealed class PlayerAnalysisRepository(LolAnalyzerDbContext dbContext) : IPlayerAnalysisRepository
{
    public async Task<PlayerAnalysisInput?> LoadInputAsync(
        string ownerPuuid,
        CancellationToken cancellationToken)
    {
        var owner = await dbContext.Players
            .AsNoTracking()
            .SingleOrDefaultAsync(player => player.Puuid == ownerPuuid, cancellationToken)
            .ConfigureAwait(false);
        if (owner is null)
        {
            return null;
        }

        var rows = await dbContext.MatchParticipants
            .AsNoTracking()
            .Where(participant => participant.Match.Participants.Any(candidate => candidate.PlayerId == owner.Id))
            .Select(participant => new AnalysisRow(
                participant.MatchId,
                participant.Match.GameEndTimestamp
                    ?? participant.Match.GameStartTimestamp
                    ?? participant.Match.GameCreation
                    ?? participant.Match.CreatedAt,
                participant.PlayerId,
                participant.TeamId,
                participant.Win))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var matches = rows
            .GroupBy(row => new { row.MatchId, row.OccurredAt })
            .Select(group =>
            {
                var ownerRow = group.Single(row => row.PlayerId == owner.Id);
                return new EncounterMatch(
                    group.Key.MatchId,
                    group.Key.OccurredAt,
                    owner.Id,
                    ownerRow.TeamId,
                    ownerRow.Win,
                    group.Select(row => new EncounterParticipant(row.PlayerId, row.TeamId)).ToArray());
            })
            .ToArray();

        return new PlayerAnalysisInput(owner.Id, matches);
    }

    public async Task ReplaceEncountersAsync(
        Guid ownerPlayerId,
        IReadOnlyCollection<PlayerEncounterAggregate> encounters,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);
        await dbContext.PlayerEncounters
            .Where(encounter => encounter.OwnerPlayerId == ownerPlayerId)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);

        dbContext.PlayerEncounters.AddRange(encounters.Select(encounter => new PlayerEncounter
        {
            OwnerPlayerId = ownerPlayerId,
            OtherPlayerId = encounter.OtherPlayerId,
            TotalMatches = encounter.TotalMatches,
            SameTeamMatches = encounter.SameTeamMatches,
            EnemyTeamMatches = encounter.EnemyTeamMatches,
            WinsTogether = encounter.WinsTogether,
            LossesTogether = encounter.LossesTogether,
            WinsAgainst = encounter.WinsAgainst,
            LossesAgainst = encounter.LossesAgainst,
            FirstSeenAt = encounter.FirstSeenAt,
            LastSeenAt = encounter.LastSeenAt,
        }));
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<PlayerSummary?> GetSummaryAsync(string puuid, CancellationToken cancellationToken)
    {
        var player = await dbContext.Players
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Puuid == puuid, cancellationToken)
            .ConfigureAwait(false);
        if (player is null)
        {
            return null;
        }

        var results = await dbContext.MatchParticipants
            .AsNoTracking()
            .Where(participant => participant.PlayerId == player.Id)
            .Select(participant => participant.Win)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var uniquePlayers = await dbContext.PlayerEncounters
            .AsNoTracking()
            .CountAsync(encounter => encounter.OwnerPlayerId == player.Id, cancellationToken)
            .ConfigureAwait(false);
        var repeatedPlayers = await dbContext.PlayerEncounters
            .AsNoTracking()
            .CountAsync(
                encounter => encounter.OwnerPlayerId == player.Id && encounter.TotalMatches >= 2,
                cancellationToken)
            .ConfigureAwait(false);
        var wins = results.Count(result => result);

        return new PlayerSummary(
            player.Puuid,
            player.GameName,
            player.TagLine,
            results.Count,
            wins,
            results.Count - wins,
            results.Count == 0 ? 0 : Math.Round(wins * 100d / results.Count, 1),
            uniquePlayers,
            repeatedPlayers);
    }

    public async Task<IReadOnlyList<PlayerEncounterView>?> GetRepeatedPlayersAsync(
        string puuid,
        CancellationToken cancellationToken)
    {
        var ownerId = await dbContext.Players
            .AsNoTracking()
            .Where(player => player.Puuid == puuid)
            .Select(player => (Guid?)player.Id)
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        if (ownerId is null)
        {
            return null;
        }

        return await dbContext.PlayerEncounters
            .AsNoTracking()
            .Where(encounter => encounter.OwnerPlayerId == ownerId && encounter.TotalMatches >= 2)
            .OrderByDescending(encounter => encounter.TotalMatches)
            .ThenByDescending(encounter => encounter.LastSeenAt)
            .Select(encounter => new PlayerEncounterView(
                encounter.OtherPlayer.Puuid,
                encounter.OtherPlayer.GameName,
                encounter.OtherPlayer.TagLine,
                encounter.TotalMatches,
                encounter.SameTeamMatches,
                encounter.EnemyTeamMatches,
                encounter.WinsTogether,
                encounter.LossesTogether,
                encounter.WinsAgainst,
                encounter.LossesAgainst,
                encounter.FirstSeenAt,
                encounter.LastSeenAt))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<PagedPlayerMatches?> GetMatchesAsync(
        string puuid,
        int page,
        int pageSize,
        int? queueId,
        CancellationToken cancellationToken)
    {
        var playerExists = await dbContext.Players
            .AsNoTracking()
            .AnyAsync(player => player.Puuid == puuid, cancellationToken)
            .ConfigureAwait(false);
        if (!playerExists)
        {
            return null;
        }

        var query = dbContext.MatchParticipants
            .AsNoTracking()
            .Where(participant => participant.Player.Puuid == puuid);
        if (queueId.HasValue)
        {
            query = query.Where(participant => participant.Match.QueueId == queueId);
        }

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await query
            .OrderByDescending(participant => participant.Match.GameStartTimestamp)
            .ThenByDescending(participant => participant.Match.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(participant => new PlayerMatchListItem(
                participant.Match.RiotMatchId,
                participant.Match.QueueId,
                participant.Match.GameStartTimestamp,
                participant.Match.GameDurationSeconds,
                participant.ChampionName,
                participant.Kills,
                participant.Deaths,
                participant.Assists,
                participant.Win))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedPlayerMatches(page, pageSize, totalCount, items);
    }

    public async Task<MatchDetail?> GetMatchDetailAsync(
        string riotMatchId,
        CancellationToken cancellationToken)
    {
        var match = await dbContext.Matches
            .AsNoTracking()
            .Include(candidate => candidate.Participants)
            .ThenInclude(participant => participant.Player)
            .SingleOrDefaultAsync(candidate => candidate.RiotMatchId == riotMatchId, cancellationToken)
            .ConfigureAwait(false);
        if (match is null)
        {
            return null;
        }

        var teams = match.Participants
            .GroupBy(participant => participant.TeamId)
            .OrderBy(group => group.Key)
            .Select(group => new MatchTeamDetail(
                group.Key,
                group.OrderBy(participant => participant.ParticipantId)
                    .Select(participant => new MatchParticipantDetail(
                        participant.Player.Puuid,
                        participant.Player.GameName,
                        participant.Player.TagLine,
                        participant.TeamId,
                        participant.ParticipantId,
                        participant.ChampionName,
                        participant.TeamPosition,
                        participant.Kills,
                        participant.Deaths,
                        participant.Assists,
                        participant.Win))
                    .ToArray()))
            .ToArray();

        return new MatchDetail(
            match.RiotMatchId,
            match.QueueId,
            match.GameStartTimestamp,
            match.GameDurationSeconds,
            teams);
    }

    private sealed record AnalysisRow(
        Guid MatchId,
        DateTimeOffset OccurredAt,
        Guid PlayerId,
        int TeamId,
        bool Win);
}
