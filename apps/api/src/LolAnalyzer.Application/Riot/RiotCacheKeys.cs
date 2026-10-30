using System.Security.Cryptography;
using System.Text;

namespace LolAnalyzer.Application.Riot;

public static class RiotCacheKeys
{
    public static string Account(string gameName, string tagLine) =>
        $"riot-account:v1:{Hash(gameName)}:{Hash(tagLine)}";

    public static string MatchIds(string puuid, int start, int count) =>
        $"riot-match-ids:v1:{Hash(puuid)}:{start}:{count}";

    public static string Ranked(string puuid) => $"riot-ranked:v1:{Hash(puuid)}";

    public static string Match(string matchId) => $"riot-match:v1:{Hash(matchId)}";

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
