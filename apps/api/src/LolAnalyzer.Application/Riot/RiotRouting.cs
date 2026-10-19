namespace LolAnalyzer.Application.Riot;

public static class RiotRouting
{
    public static string PlatformForMatch(string matchId, string? requested, string configured)
    {
        if (!string.IsNullOrWhiteSpace(requested)) return requested;
        var separator = matchId.IndexOf('_', StringComparison.Ordinal);
        if (separator > 0)
        {
            var prefix = matchId[..separator];
            try { _ = Regional(prefix); return prefix; }
            catch (ArgumentException) { /* Non-standard IDs fall back to the configured platform. */ }
        }
        return configured;
    }

    public static string Regional(string platform) => platform.Trim().ToUpperInvariant() switch
    {
        "NA1" or "BR1" or "LA1" or "LA2" => "americas",
        "EUW1" or "EUN1" or "TR1" or "RU" => "europe",
        "KR" or "JP1" => "asia",
        "OC1" or "PH2" or "SG2" or "TH2" or "TW2" or "VN2" => "sea",
        _ => throw new ArgumentException("Unsupported platform region.", nameof(platform)),
    };
}
