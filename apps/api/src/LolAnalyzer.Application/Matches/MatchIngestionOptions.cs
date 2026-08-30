namespace LolAnalyzer.Application.Matches;

public sealed class MatchIngestionOptions
{
    public int RequestConcurrency { get; set; } = 3;
}
