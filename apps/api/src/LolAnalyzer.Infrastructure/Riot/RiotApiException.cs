using System.Net;

namespace LolAnalyzer.Infrastructure.Riot;

public sealed class RiotApiException(HttpStatusCode statusCode)
    : Exception("The Riot API request did not complete successfully.")
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}
