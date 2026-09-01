using System.Text.Json;
using LolAnalyzer.Application.Matches;
using LolAnalyzer.Application.Riot;
using LolAnalyzer.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LolAnalyzer.Infrastructure.Persistence;

public sealed class MatchRepository(LolAnalyzerDbContext dbContext) : IMatchRepository
{
    public async Task<IReadOnlySet<string>> FindExistingRiotMatchIdsAsync(
        IReadOnlyCollection<string> riotMatchIds,
        CancellationToken cancellationToken)
    {
        if (riotMatchIds.Count == 0)
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }

        var existing = await dbContext.Matches
            .AsNoTracking()
            .Where(match => riotMatchIds.Contains(match.RiotMatchId))
            .Select(match => match.RiotMatchId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new HashSet<string>(existing, StringComparer.Ordinal);
    }

    public async Task<bool> SaveIfMissingAsync(
        RiotMatchData match,
        string platformRegion,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);
        await dbContext.Database
            .ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtextextended({match.RiotMatchId}, 0));",
                cancellationToken)
            .ConfigureAwait(false);
        if (await dbContext.Matches.AnyAsync(
                existing => existing.RiotMatchId == match.RiotMatchId,
                cancellationToken).ConfigureAwait(false))
        {
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return false;
        }

        var now = DateTimeOffset.UtcNow;
        var participantsByPuuid = match.Participants
            .GroupBy(participant => participant.Puuid, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var participantPuuids = participantsByPuuid.Keys.ToArray();
        var knownPlayers = await dbContext.Players
            .Where(player => participantPuuids.Contains(player.Puuid))
            .ToDictionaryAsync(player => player.Puuid, StringComparer.Ordinal, cancellationToken)
            .ConfigureAwait(false);

        foreach (var participant in participantsByPuuid.Values)
        {
            if (knownPlayers.TryGetValue(participant.Puuid, out var player))
            {
                if (!string.IsNullOrWhiteSpace(participant.GameName))
                {
                    player.GameName = participant.GameName;
                }

                if (!string.IsNullOrWhiteSpace(participant.TagLine))
                {
                    player.TagLine = participant.TagLine;
                }

                player.PlatformRegion = platformRegion;
                player.UpdatedAt = now;
                player.LastSeenAt = now;
                continue;
            }

            player = new Player
            {
                Puuid = participant.Puuid,
                GameName = participant.GameName ?? "Unknown",
                TagLine = participant.TagLine ?? "Unknown",
                PlatformRegion = platformRegion,
                CreatedAt = now,
                UpdatedAt = now,
                LastSeenAt = now,
            };
            knownPlayers.Add(player.Puuid, player);
            dbContext.Players.Add(player);
        }

        var persistedMatch = new Match
        {
            RiotMatchId = match.RiotMatchId,
            QueueId = match.QueueId,
            GameCreation = match.GameCreation,
            GameStartTimestamp = match.GameStartTimestamp,
            GameEndTimestamp = match.GameEndTimestamp,
            GameDurationSeconds = match.GameDurationSeconds,
            GameVersion = match.GameVersion,
            RawData = JsonDocument.Parse(match.RawJson),
            CreatedAt = now,
        };
        dbContext.Matches.Add(persistedMatch);

        foreach (var participant in participantsByPuuid.Values)
        {
            dbContext.MatchParticipants.Add(new MatchParticipant
            {
                Match = persistedMatch,
                Player = knownPlayers[participant.Puuid],
                TeamId = participant.TeamId,
                ParticipantId = participant.ParticipantId,
                ChampionId = participant.ChampionId,
                ChampionName = participant.ChampionName,
                TeamPosition = participant.TeamPosition,
                IndividualPosition = participant.IndividualPosition,
                Kills = participant.Kills,
                Deaths = participant.Deaths,
                Assists = participant.Assists,
                Win = participant.Win,
                GoldEarned = participant.GoldEarned,
                TotalDamageDealtToChampions = participant.TotalDamageDealtToChampions,
                VisionScore = participant.VisionScore,
                Cs = participant.Cs,
                CreatedAt = now,
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }
}
