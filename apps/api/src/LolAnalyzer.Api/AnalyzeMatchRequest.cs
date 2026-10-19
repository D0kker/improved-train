namespace LolAnalyzer.Api;

public sealed record AnalyzeMatchRequest(string OwnerPuuid, string? PlatformRegion = null);
