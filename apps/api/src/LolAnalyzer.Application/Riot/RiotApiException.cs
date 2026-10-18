using System.Net;

namespace LolAnalyzer.Application.Riot;

public sealed class RiotApiException(HttpStatusCode statusCode)
    : Exception("The Riot API request did not complete successfully.")
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}
