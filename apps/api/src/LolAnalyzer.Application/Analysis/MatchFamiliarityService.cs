using LolAnalyzer.Application.Observability;

namespace LolAnalyzer.Application.Analysis;

public sealed class MatchFamiliarityService(
    IPlayerAnalysisRepository repository,
    OperationalMetrics metrics)
{
    public async Task<MatchFamiliarityResult?> CalculateAsync(
        string ownerPuuid,
        string targetRiotMatchId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerPuuid);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetRiotMatchId);

        var input = await repository
            .LoadFamiliarityInputAsync(ownerPuuid, targetRiotMatchId, cancellationToken)
            .ConfigureAwait(false);

        if (input is null)
        {
            return null;
        }

        var result = MatchFamiliarityCalculator.Calculate(input);
        metrics.RecordMatchFamiliarity(result.KnownPlayers);
        return result;
    }
}
