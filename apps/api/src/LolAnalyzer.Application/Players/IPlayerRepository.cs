using LolAnalyzer.Domain.Entities;

namespace LolAnalyzer.Application.Players;

public interface IPlayerRepository
{
    Task<Player> UpsertAsync(
        string puuid,
        string gameName,
        string tagLine,
        string platformRegion,
        CancellationToken cancellationToken);
}
