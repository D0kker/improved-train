namespace LolAnalyzer.Application.Analysis;

public sealed class RepeatedPlayerAnalysisService(IPlayerAnalysisRepository repository)
{
    public async Task<PlayerAnalysisResult?> RebuildAsync(
        string ownerPuuid,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerPuuid);

        var input = await repository.LoadInputAsync(ownerPuuid, cancellationToken).ConfigureAwait(false);
        if (input is null)
        {
            return null;
        }

        var encounters = RepeatedPlayerAnalyzer.Analyze(input);
        await repository
            .ReplaceEncountersAsync(input.OwnerPlayerId, encounters, cancellationToken)
            .ConfigureAwait(false);

        return new PlayerAnalysisResult(input.OwnerPlayerId, input.Matches.Count, encounters.Count);
    }
}
