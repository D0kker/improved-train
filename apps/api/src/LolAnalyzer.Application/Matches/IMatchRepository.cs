using LolAnalyzer.Application.Riot;

namespace LolAnalyzer.Application.Matches;

public interface IMatchRepository
{
    Task<IReadOnlySet<string>> FindExistingRiotMatchIdsAsync(
        IReadOnlyCollection<string> riotMatchIds,
        CancellationToken cancellationToken);

    Task<bool> SaveIfMissingAsync(
        RiotMatchData match,
        string platformRegion,
        CancellationToken cancellationToken);
}
