namespace LolAnalyzer.Application.Analysis;

public sealed class PlayerRelationshipAnalysisOptions
{
    public const string SectionName = "RelationshipAnalysis";

    public int ReadBatchSize { get; init; } = 200;

    public int WriteBatchSize { get; init; } = 500;

    public void Validate()
    {
        ValidateBatchSize(ReadBatchSize, nameof(ReadBatchSize));
        ValidateBatchSize(WriteBatchSize, nameof(WriteBatchSize));
    }

    private static void ValidateBatchSize(int value, string name)
    {
        if (value is < 1 or > 1_000)
        {
            throw new ArgumentOutOfRangeException(name, value, "Batch sizes must be between 1 and 1000.");
        }
    }
}
