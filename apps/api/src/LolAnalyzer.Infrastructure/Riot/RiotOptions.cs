namespace LolAnalyzer.Infrastructure.Riot;

public sealed class RiotOptions
{
    public const string SectionName = "Riot";

    public string ApiKey { get; set; } = string.Empty;

    public string PlatformRegion { get; set; } = "la1";

    public string RegionalRouting { get; set; } = "americas";

    public int RequestTimeoutSeconds { get; set; } = 10;

    public int RankedRequestTimeoutSeconds { get; set; } = 5;

    public int RequestConcurrency { get; set; } = 3;

    public int ShortWindowRequests { get; set; } = 20;

    public int ShortWindowSeconds { get; set; } = 1;

    public int LongWindowRequests { get; set; } = 100;

    public int LongWindowSeconds { get; set; } = 120;

    public int MaxRetryAttempts { get; set; } = 2;

    public int BaseRetryDelayMilliseconds { get; set; } = 250;

    public int MaxRetryDelaySeconds { get; set; } = 120;

    public void Validate()
    {
        if (ShortWindowRequests is < 1 or > 10000 || LongWindowRequests is < 1 or > 10000
            || ShortWindowSeconds is < 1 or > 3600 || LongWindowSeconds is < 1 or > 3600)
        {
            throw new InvalidOperationException("Riot quota windows require 1–10000 requests and 1–3600 seconds.");
        }
        if (RankedRequestTimeoutSeconds is < 1 or > 5)
            throw new InvalidOperationException("Ranked request timeout must be between 1 and 5 seconds.");
        if (RequestTimeoutSeconds <= 0)
        {
            throw new InvalidOperationException("Riot request timeout must be positive.");
        }

        if (RequestConcurrency is < 1 or > 5)
        {
            throw new InvalidOperationException("Riot request concurrency must be between 1 and 5.");
        }

        if (MaxRetryAttempts is < 0 or > 5)
        {
            throw new InvalidOperationException("Riot retry attempts must be between 0 and 5.");
        }

        if (BaseRetryDelayMilliseconds is < 1 or > 10000)
        {
            throw new InvalidOperationException("Riot base retry delay must be between 1 and 10000 milliseconds.");
        }

        if (MaxRetryDelaySeconds is < 1 or > 900)
        {
            throw new InvalidOperationException("Riot maximum retry delay must be between 1 and 900 seconds.");
        }
    }

    public Uri GetRegionalBaseUri()
    {
        var routing = RegionalRouting.Trim().ToLowerInvariant();
        if (routing is not ("americas" or "asia" or "europe" or "sea"))
        {
            throw new InvalidOperationException("Riot:RegionalRouting must be a valid Riot regional routing value.");
        }

        return new Uri($"https://{routing}.api.riotgames.com/", UriKind.Absolute);
    }

    public Uri GetPlatformBaseUri()
    {
        var platform = PlatformRegion.Trim().ToLowerInvariant();
        if (platform.Length is < 2 or > 8 || platform.Any(character => !char.IsLetterOrDigit(character)))
        {
            throw new InvalidOperationException("Riot:PlatformRegion must be a valid platform routing value.");
        }

        return new Uri($"https://{platform}.api.riotgames.com/", UriKind.Absolute);
    }
}
