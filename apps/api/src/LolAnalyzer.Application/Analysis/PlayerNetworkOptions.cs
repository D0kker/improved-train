namespace LolAnalyzer.Application.Analysis;

public sealed class PlayerNetworkOptions
{
    public const string SectionName = "PlayerNetwork";

    public int MaximumNodes { get; init; } = 50;

    public int MaximumEdges { get; init; } = 100;

    public void Validate()
    {
        if (MaximumNodes is < 1 or > 101 || MaximumEdges is < 1 or > 100)
        {
            throw new InvalidOperationException(
                "Player network limits must allow 1-101 nodes and 1-100 edges.");
        }
    }
}
