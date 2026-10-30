namespace LolAnalyzer.Application.Riot;

/// <summary>A temporary League-V4 snapshot, independent of match analysis.</summary>
public sealed record RiotRankedProfile(IReadOnlyList<RiotRankedEntry> Entries, DateTimeOffset ObservedAt);

public sealed record RiotRankedEntry(string QueueType, string Tier, string Division, int LeaguePoints);

public static class RankedPlatforms
{
    // Intersection of the current League-V4 platform catalog and the existing application routing.
    // ME1 is not added to global routing here; retired PH2/TH2 are deliberately excluded.
    public static bool IsSupported(string platform) => platform.Trim().ToUpperInvariant() is
        "BR1" or "EUN1" or "EUW1" or "JP1" or "KR" or "LA1" or "LA2" or "NA1" or "OC1"
        or "RU" or "SG2" or "TR1" or "TW2" or "VN2";
}
