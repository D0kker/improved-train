using LolAnalyzer.Domain.Entities;

namespace LolAnalyzer.Application.Players;

public interface IPlayerRepository
{
    Task<Player?> FindByRiotIdAsync(
        string gameName,
        string tagLine,
        CancellationToken cancellationToken);

    Task<Player> UpsertAsync(
        string puuid,
        string gameName,
        string tagLine,
        string platformRegion,
        CancellationToken cancellationToken);
}
