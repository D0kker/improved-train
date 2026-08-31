using LolAnalyzer.Application.Riot;
using LolAnalyzer.Domain.Entities;

namespace LolAnalyzer.Application.Players;

public sealed class PlayerLookupService(IRiotApiClient riotApiClient, IPlayerRepository playerRepository)
{
    public async Task<Player?> FindByRiotIdAsync(
        string gameName,
        string tagLine,
        string platformRegion,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameName);
        ArgumentException.ThrowIfNullOrWhiteSpace(tagLine);
        ArgumentException.ThrowIfNullOrWhiteSpace(platformRegion);

        var localPlayer = await playerRepository
            .FindByRiotIdAsync(gameName, tagLine, cancellationToken)
            .ConfigureAwait(false);
        if (localPlayer is not null)
        {
            return localPlayer;
        }

        var account = await riotApiClient
            .GetAccountByRiotIdAsync(gameName, tagLine, cancellationToken)
            .ConfigureAwait(false);

        return account is null
            ? null
            : await playerRepository.UpsertAsync(
                    account.Puuid,
                    account.GameName,
                    account.TagLine,
                    platformRegion,
                    cancellationToken)
                .ConfigureAwait(false);
    }
}
