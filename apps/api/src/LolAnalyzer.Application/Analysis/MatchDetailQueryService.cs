namespace LolAnalyzer.Application.Analysis;

public sealed class MatchDetailQueryService(
    IPlayerAnalysisRepository analysisRepository,
    MatchPremadeGroupService premadeGroupService)
{
    public async Task<MatchDetail?> GetAsync(string riotMatchId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(riotMatchId);
        var detail = await analysisRepository
            .GetMatchDetailAsync(riotMatchId, cancellationToken)
            .ConfigureAwait(false);
        if (detail is null)
        {
            return null;
        }

        var groups = await premadeGroupService
            .GetAsync(riotMatchId, cancellationToken)
            .ConfigureAwait(false);
        return detail with { PremadeGroups = groups ?? [] };
    }
}
