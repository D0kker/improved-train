namespace LolAnalyzer.Api;

public static partial class AnalysisLog
{
    [LoggerMessage(EventId = 210, Level = LogLevel.Information,
        Message = "On-demand analysis completed. Kind={Kind} Participants={Participants} MatchIds={MatchIds} UniqueMatches={UniqueMatches} Recovered={Recovered} Failures={Failures} Coverage={Coverage}")]
    public static partial void Completed(ILogger logger, string kind, int participants, int matchIds, int uniqueMatches,
        int recovered, int failures, string coverage);
}
