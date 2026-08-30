namespace LolAnalyzer.Infrastructure.Riot;

public sealed class RiotOptions
{
    public const string SectionName = "Riot";

    public string ApiKey { get; set; } = string.Empty;

    public string PlatformRegion { get; set; } = "la1";

    public string RegionalRouting { get; set; } = "americas";

    public int RequestTimeoutSeconds { get; set; } = 10;

    public int RequestConcurrency { get; set; } = 3;

    public Uri GetRegionalBaseUri()
    {
        var routing = RegionalRouting.Trim().ToLowerInvariant();
        if (routing is not ("americas" or "asia" or "europe" or "sea"))
        {
            throw new InvalidOperationException("Riot:RegionalRouting must be a valid Riot regional routing value.");
        }

        return new Uri($"https://{routing}.api.riotgames.com/", UriKind.Absolute);
    }
}
