using System.Security.Cryptography;
using System.Text;

namespace LolAnalyzer.Application.Caching;

public static class PlayerCacheKeys
{
    public static string Summary(string puuid) => $"player-summary:v1:{Hash(puuid)}";

    public static string Tag(string puuid) => $"player:v1:{Hash(puuid)}";

    private static string Hash(string puuid)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(puuid);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(puuid))).ToLowerInvariant();
    }
}
