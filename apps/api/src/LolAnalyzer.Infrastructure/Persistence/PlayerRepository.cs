using LolAnalyzer.Application.Players;
using LolAnalyzer.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LolAnalyzer.Infrastructure.Persistence;

public sealed class PlayerRepository(LolAnalyzerDbContext dbContext) : IPlayerRepository
{
    public Task<Player?> FindByRiotIdAsync(
        string gameName,
        string tagLine,
        CancellationToken cancellationToken) =>
        dbContext.Players
            .AsNoTracking()
            .Where(player => player.GameName == gameName && player.TagLine == tagLine)
            .OrderByDescending(player => player.UpdatedAt)
            .ThenBy(player => player.Puuid)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<Player> UpsertAsync(
        string puuid,
        string gameName,
        string tagLine,
        string platformRegion,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var player = await dbContext.Players.SingleOrDefaultAsync(
            candidate => candidate.Puuid == puuid,
            cancellationToken).ConfigureAwait(false);

        if (player is null)
        {
            player = new Player
            {
                Puuid = puuid,
                GameName = gameName,
                TagLine = tagLine,
                PlatformRegion = platformRegion,
                CreatedAt = now,
                UpdatedAt = now,
                LastSeenAt = now,
            };
            dbContext.Players.Add(player);
        }
        else
        {
            player.GameName = gameName;
            player.TagLine = tagLine;
            player.PlatformRegion = platformRegion;
            player.UpdatedAt = now;
            player.LastSeenAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return player;
    }
}
