using System.Net;
using LolAnalyzer.Application.Riot;
using LolAnalyzer.Infrastructure.Riot;

namespace LolAnalyzer.Api;

public static class RankedEndpointMapping
{
    public static void MapRankedEndpoint(this WebApplication app)
    {
        app.MapGet("/api/v1/players/{puuid}/ranked", async Task<IResult> (
            string puuid, string? platformRegion, IRiotApiClient riot, RiotOptions options, CancellationToken token) =>
        {
            var platform = platformRegion ?? options.PlatformRegion;
            if (!RankedPlatforms.IsSupported(platform))
                return Results.Problem(statusCode: 400, title: "Ranked information is unavailable for this platform.");
            using var budget = CancellationTokenSource.CreateLinkedTokenSource(token);
            budget.CancelAfter(TimeSpan.FromSeconds(options.RankedRequestTimeoutSeconds));
            try
            {
                return Results.Ok(await riot.ForPlatform(platform).GetRankedAsync(puuid, budget.Token).ConfigureAwait(false));
            }
            catch (OperationCanceledException) when (!token.IsCancellationRequested)
            {
                return Results.Problem(statusCode: 504, title: "Ranked information request timed out.");
            }
            catch (RiotApiException error)
            {
                var status = error.StatusCode switch
                {
                    HttpStatusCode.NotFound => 404,
                    HttpStatusCode.TooManyRequests => 429,
                    HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or HttpStatusCode.BadGateway => 502,
                    HttpStatusCode.GatewayTimeout => 504,
                    _ => 503,
                };
                return Results.Problem(statusCode: status, title: "Ranked information is temporarily unavailable.");
            }
            catch (HttpRequestException)
            {
                return Results.Problem(statusCode: 503, title: "Ranked information is temporarily unavailable.");
            }
        }).Produces<RiotRankedProfile>();
    }
}
