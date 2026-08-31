namespace LolAnalyzer.Application.Analysis;

public sealed class MatchDetailQueryService(
    IPlayerAnalysisRepository analysisRepository,
    MatchPremadeGroupService premadeGroupService,
    MatchFamiliarityService familiarityService)
{
    public async Task<MatchDetail?> GetAsync(
        string riotMatchId,
        string? ownerPuuid,
        CancellationToken cancellationToken)
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
        var familiarity = string.IsNullOrWhiteSpace(ownerPuuid)
            ? null
            : await familiarityService
                .CalculateAsync(ownerPuuid, riotMatchId, cancellationToken)
                .ConfigureAwait(false);
        return detail with
        {
            PremadeGroups = groups ?? [],
            Familiarity = familiarity is null
                ? null
                : new MatchFamiliarityView(
                    familiarity.KnownPlayers,
                    familiarity.UnknownPlayers,
                    familiarity.EvaluablePlayers,
                    familiarity.FamiliarityPercentage,
                    familiarity.Status.ToString()),
        };
    }
}
