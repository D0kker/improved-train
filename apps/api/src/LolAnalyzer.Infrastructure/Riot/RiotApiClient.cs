using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using LolAnalyzer.Application.Observability;
using LolAnalyzer.Application.Riot;

namespace LolAnalyzer.Infrastructure.Riot;

public sealed class RiotApiClient(
    HttpClient httpClient,
    RiotOptions options,
    IRiotRateLimiter rateLimiter,
    TimeProvider timeProvider,
    OperationalMetrics metrics) : IRiotApiClient
{
    public async Task<RiotAccount?> GetAccountByRiotIdAsync(
        string gameName,
        string tagLine,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameName);
        ArgumentException.ThrowIfNullOrWhiteSpace(tagLine);

        var path = $"riot/account/v1/accounts/by-riot-id/{Uri.EscapeDataString(gameName)}/{Uri.EscapeDataString(tagLine)}";
        using var response = await SendGetAsync(path, "account", cancellationToken).ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new RiotApiException(response.StatusCode);
        }

        var payload = await response.Content.ReadFromJsonAsync<AccountResponse>(
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (payload is null || string.IsNullOrWhiteSpace(payload.Puuid))
        {
            throw new RiotApiException(HttpStatusCode.BadGateway);
        }

        return new RiotAccount(payload.Puuid, payload.GameName ?? gameName, payload.TagLine ?? tagLine);
    }

    public async Task<IReadOnlyList<string>> GetMatchIdsAsync(
        string puuid,
        int start,
        int count,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(puuid);
        ArgumentOutOfRangeException.ThrowIfNegative(start);
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, 100);

        var path = $"lol/match/v5/matches/by-puuid/{Uri.EscapeDataString(puuid)}/ids?start={start}&count={count}";
        using var response = await SendGetAsync(path, "match_ids", cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new RiotApiException(response.StatusCode);
        }

        var matchIds = await response.Content.ReadFromJsonAsync<List<string>>(
            cancellationToken: cancellationToken).ConfigureAwait(false);
        return matchIds ?? [];
    }

    public async Task<RiotMatchData?> GetMatchAsync(string riotMatchId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(riotMatchId);

        var path = $"lol/match/v5/matches/{Uri.EscapeDataString(riotMatchId)}";
        using var response = await SendGetAsync(path, "match", cancellationToken).ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new RiotApiException(response.StatusCode);
        }

        var rawJson = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return ParseMatch(rawJson);
        }
        catch (JsonException)
        {
            throw new RiotApiException(HttpStatusCode.BadGateway);
        }
    }

    private async Task<HttpResponseMessage> SendGetAsync(
        string path,
        string endpoint,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new InvalidOperationException("RIOT_API_KEY must be configured at runtime before calling Riot.");
        }

        for (var attempt = 0; ; attempt++)
        {
            HttpResponseMessage response;
            using (await rateLimiter.AcquireAsync(cancellationToken).ConfigureAwait(false))
            using (var request = new HttpRequestMessage(HttpMethod.Get, path))
            {
                request.Headers.TryAddWithoutValidation("X-Riot-Token", options.ApiKey);
                response = await httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken).ConfigureAwait(false);
            }

            metrics.RecordRiotRequest(endpoint, (int)response.StatusCode);

            if (response.StatusCode != HttpStatusCode.TooManyRequests)
            {
                return response;
            }

            var retryAfter = GetRetryDelay(response, attempt);
            rateLimiter.RegisterRetryAfter(retryAfter);

            if (attempt >= options.MaxRetryAttempts || retryAfter > TimeSpan.FromSeconds(options.MaxRetryDelaySeconds))
            {
                return response;
            }

            response.Dispose();
        }
    }

    private TimeSpan GetRetryDelay(HttpResponseMessage response, int attempt)
    {
        var retryAfter = response.Headers.RetryAfter;
        if (retryAfter?.Delta is { } delta)
        {
            return delta > TimeSpan.Zero ? delta : TimeSpan.FromMilliseconds(options.BaseRetryDelayMilliseconds);
        }

        if (retryAfter?.Date is { } date)
        {
            var delay = date - timeProvider.GetUtcNow();
            return delay > TimeSpan.Zero ? delay : TimeSpan.FromMilliseconds(options.BaseRetryDelayMilliseconds);
        }

        var exponent = Math.Min(attempt, 30);
        var delayMilliseconds = options.BaseRetryDelayMilliseconds * Math.Pow(2, exponent);
        return TimeSpan.FromMilliseconds(Math.Min(
            delayMilliseconds,
            TimeSpan.FromSeconds(options.MaxRetryDelaySeconds).TotalMilliseconds));
    }

    private static RiotMatchData ParseMatch(string rawJson)
    {
        using var document = JsonDocument.Parse(rawJson);
        var root = document.RootElement;
        var metadata = GetRequiredObject(root, "metadata");
        var info = GetRequiredObject(root, "info");
        var riotMatchId = GetRequiredString(metadata, "matchId");
        var gameEndTimestamp = GetTimestamp(info, "gameEndTimestamp")
            ?? throw new JsonException("Match response does not represent a completed game.");
        var participants = GetRequiredArray(info, "participants")
            .EnumerateArray()
            .Select(ParseParticipant)
            .ToArray();

        if (participants.Length == 0 || participants.Select(participant => participant.Puuid).Distinct(StringComparer.Ordinal).Count() != participants.Length)
        {
            throw new JsonException("Match response has invalid participants.");
        }

        return new RiotMatchData(
            riotMatchId,
            GetNullableInt(info, "queueId"),
            GetTimestamp(info, "gameCreation"),
            GetTimestamp(info, "gameStartTimestamp"),
            gameEndTimestamp,
            GetDurationSeconds(info),
            GetNullableString(info, "gameVersion"),
            rawJson,
            participants);
    }

    private static RiotMatchParticipantData ParseParticipant(JsonElement participant) => new(
        GetRequiredString(participant, "puuid"),
        GetNullableString(participant, "riotIdGameName"),
        GetNullableString(participant, "riotIdTagline"),
        GetRequiredInt(participant, "teamId"),
        GetRequiredInt(participant, "participantId"),
        GetRequiredInt(participant, "championId"),
        GetRequiredString(participant, "championName"),
        GetNullableString(participant, "teamPosition"),
        GetNullableString(participant, "individualPosition"),
        GetRequiredInt(participant, "kills"),
        GetRequiredInt(participant, "deaths"),
        GetRequiredInt(participant, "assists"),
        GetRequiredBoolean(participant, "win"),
        GetRequiredInt(participant, "goldEarned"),
        GetRequiredInt(participant, "totalDamageDealtToChampions"),
        GetRequiredInt(participant, "visionScore"),
        GetRequiredInt(participant, "totalMinionsKilled") + GetRequiredInt(participant, "neutralMinionsKilled"));

    private static JsonElement GetRequiredObject(JsonElement source, string propertyName) =>
        source.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.Object
            ? property
            : throw new JsonException($"Missing object property: {propertyName}.");

    private static JsonElement GetRequiredArray(JsonElement source, string propertyName) =>
        source.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.Array
            ? property
            : throw new JsonException($"Missing array property: {propertyName}.");

    private static string GetRequiredString(JsonElement source, string propertyName) =>
        GetNullableString(source, propertyName) is { Length: > 0 } value
            ? value
            : throw new JsonException($"Missing string property: {propertyName}.");

    private static string? GetNullableString(JsonElement source, string propertyName) =>
        source.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    private static int GetRequiredInt(JsonElement source, string propertyName) =>
        GetNullableInt(source, propertyName)
        ?? throw new JsonException($"Missing integer property: {propertyName}.");

    private static int? GetNullableInt(JsonElement source, string propertyName)
    {
        if (!source.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Number)
        {
            return null;
        }

        return property.TryGetInt32(out var integer) ? integer : null;
    }

    private static int? GetDurationSeconds(JsonElement source)
    {
        var duration = GetNullableInt(source, "gameDuration");
        if (duration.HasValue)
        {
            return duration;
        }

        return source.TryGetProperty("gameDuration", out var property)
            && property.ValueKind == JsonValueKind.Number
            && property.TryGetDouble(out var fractionalDuration)
            && fractionalDuration >= 0
            && fractionalDuration <= int.MaxValue
                ? (int)Math.Round(fractionalDuration, MidpointRounding.AwayFromZero)
                : null;
    }

    private static bool GetRequiredBoolean(JsonElement source, string propertyName) =>
        source.TryGetProperty(propertyName, out var property) && property.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? property.GetBoolean()
            : throw new JsonException($"Missing boolean property: {propertyName}.");

    private static DateTimeOffset? GetTimestamp(JsonElement source, string propertyName) =>
        source.TryGetProperty(propertyName, out var property)
        && property.ValueKind == JsonValueKind.Number
        && property.TryGetInt64(out var timestamp)
        && timestamp > 0
            ? DateTimeOffset.FromUnixTimeMilliseconds(timestamp)
            : null;

    private sealed record AccountResponse(
        [property: JsonPropertyName("puuid")] string? Puuid,
        [property: JsonPropertyName("gameName")] string? GameName,
        [property: JsonPropertyName("tagLine")] string? TagLine);
}
