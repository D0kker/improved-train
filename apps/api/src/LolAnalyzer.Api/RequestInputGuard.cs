namespace LolAnalyzer.Api;

public static class RequestInputGuard
{
    public static bool IsValid(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api/v1")) return true;
        if (context.Request.Path.Value?.Length > 1024 || context.Request.QueryString.Value?.Length > 1024) return false;
        foreach (var (name, value) in context.Request.RouteValues)
        {
            var text = value?.ToString();
            var limit = name switch { "gameName" => 64, "tagLine" => 16, "puuid" or "ownerPuuid" => 128, "matchId" => 64, _ => 128 };
            if (text is not null && (string.IsNullOrWhiteSpace(text) || text.Length > limit || text.Any(char.IsControl))) return false;
        }
        foreach (var (_, values) in context.Request.Query)
            if (values.Count != 1 || values.Any(value => value is null || value.Length > 128 || value.Any(char.IsControl))) return false;
        return true;
    }
}
