namespace LolAnalyzer.Application.Analysis;

public sealed class MatchFamiliarityService(IPlayerAnalysisRepository repository)
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

        return input is null ? null : MatchFamiliarityCalculator.Calculate(input);
    }
}
