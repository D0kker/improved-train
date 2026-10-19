using System.Net;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using LolAnalyzer.Application.Caching;
using LolAnalyzer.Application.Observability;
using LolAnalyzer.Application.Riot;

namespace LolAnalyzer.Infrastructure.Riot;

public sealed class RiotApiClient(
    HttpClient httpClient,
    RiotOptions options,
    IRiotRateLimiter rateLimiter,
    TimeProvider timeProvider,
    OperationalMetrics metrics,
    IRiotResponseCache responseCache,
    CacheOptions cacheOptions) : IRiotApiClient
{
    public IRiotApiClient ForPlatform(string platformRegion) => new RiotApiClient(
        httpClient,
        new RiotOptions
        {
            ApiKey = options.ApiKey,
            PlatformRegion = platformRegion.Trim().ToLowerInvariant(),
            RegionalRouting = RiotRouting.Regional(platformRegion),
            RequestTimeoutSeconds = options.RequestTimeoutSeconds,
            RequestConcurrency = options.RequestConcurrency,
            ShortWindowRequests = options.ShortWindowRequests,
            ShortWindowSeconds = options.ShortWindowSeconds,
            LongWindowRequests = options.LongWindowRequests,
            LongWindowSeconds = options.LongWindowSeconds,
            MaxRetryAttempts = options.MaxRetryAttempts,
            BaseRetryDelayMilliseconds = options.BaseRetryDelayMilliseconds,
            MaxRetryDelaySeconds = options.MaxRetryDelaySeconds,
        }, rateLimiter, timeProvider, metrics, responseCache, cacheOptions);

    public Task<RiotAccount?> GetAccountByRiotIdAsync(
        string gameName,
        string tagLine,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameName);
        ArgumentException.ThrowIfNullOrWhiteSpace(tagLine);

        return responseCache.GetOrCreateAsync(
            $"{options.RegionalRouting}:{RiotCacheKeys.Account(gameName, tagLine)}",
            TimeSpan.FromSeconds(cacheOptions.PlayerIdentityTtlSeconds),
            "riot-identity",
            token => FetchAccountByRiotIdAsync(gameName, tagLine, token),
            cancellationToken);
    }

    private async Task<RiotAccount?> FetchAccountByRiotIdAsync(
        string gameName,
        string tagLine,
        CancellationToken cancellationToken)
    {

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

        var cached = await responseCache.GetOrCreateAsync(
            $"{options.RegionalRouting}:{RiotCacheKeys.MatchIds(puuid, start, count)}",
            TimeSpan.FromSeconds(cacheOptions.RecentMatchIdsTtlSeconds),
            "riot-match-ids",
            token => FetchMatchIdsAsync(puuid, start, count, token),
            cancellationToken).ConfigureAwait(false);
        return cached ?? [];
    }

    public async Task<IReadOnlyList<string>> GetMatchIdsBeforeAsync(
        string puuid, int count, long endTime, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(puuid);
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, 100);
        ArgumentOutOfRangeException.ThrowIfNegative(endTime);
        var cached = await responseCache.GetOrCreateAsync(
            $"{options.RegionalRouting}:{RiotCacheKeys.MatchIds(puuid, 0, count)}:before:{endTime}",
            TimeSpan.FromSeconds(cacheOptions.RecentMatchIdsTtlSeconds),
            "riot-match-ids",
            token => FetchMatchIdsAsync(puuid, 0, count, token, endTime), cancellationToken).ConfigureAwait(false);
        return cached ?? [];
    }

    private async Task<List<string>?> FetchMatchIdsAsync(
        string puuid,
        int start,
        int count,
        CancellationToken cancellationToken,
        long? endTime = null)
    {
        var path = $"lol/match/v5/matches/by-puuid/{Uri.EscapeDataString(puuid)}/ids?start={start}&count={count}";
        if (endTime.HasValue) path += $"&endTime={endTime.Value}";
        using var response = await SendGetAsync(path, "match_ids", cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new RiotApiException(response.StatusCode);
        }

        var matchIds = await response.Content.ReadFromJsonAsync<List<string>>(
            cancellationToken: cancellationToken).ConfigureAwait(false);
        if (matchIds?.Count > count) throw new RiotApiException(HttpStatusCode.BadGateway);
        return matchIds ?? [];
    }

    public Task<RiotMatchData?> GetMatchAsync(string riotMatchId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(riotMatchId);

        return responseCache.GetOrCreateAsync(
            $"{options.RegionalRouting}:{RiotCacheKeys.Match(riotMatchId)}",
            TimeSpan.FromSeconds(cacheOptions.MatchDetailsTtlSeconds),
            "riot-match-details",
            token => FetchMatchAsync(riotMatchId, token),
            cancellationToken);
    }

    private async Task<RiotMatchData?> FetchMatchAsync(string riotMatchId, CancellationToken cancellationToken)
    {

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
        var started = Stopwatch.GetTimestamp();
        try
        {
            return ParseMatch(rawJson);
        }
        catch (JsonException)
        {
            throw new RiotApiException(HttpStatusCode.BadGateway);
        }
        finally
        {
            metrics.RecordStage("riot.parse", Stopwatch.GetElapsedTime(started));
        }
    }

    public async Task<RiotActiveGame?> GetActiveGameAsync(
        string puuid,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(puuid);

        var summonerPath = $"lol/summoner/v4/summoners/by-puuid/{Uri.EscapeDataString(puuid)}";
        using var summonerResponse = await SendGetAsync(
            summonerPath,
            "summoner",
            cancellationToken,
            options.GetPlatformBaseUri())
            .ConfigureAwait(false);
        if (summonerResponse.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!summonerResponse.IsSuccessStatusCode)
        {
            throw new RiotApiException(summonerResponse.StatusCode);
        }

        var summoner = await summonerResponse.Content.ReadFromJsonAsync<SummonerResponse>(
            cancellationToken: cancellationToken).ConfigureAwait(false);
        if (summoner is null)
        {
            throw new RiotApiException(HttpStatusCode.BadGateway);
        }

        var spectatorIdentity = !string.IsNullOrWhiteSpace(summoner.Id)
            ? summoner.Id
            : summoner.Puuid;
        if (string.IsNullOrWhiteSpace(spectatorIdentity))
        {
            throw new RiotApiException(HttpStatusCode.BadGateway);
        }

        var activePath = $"lol/spectator/v5/active-games/by-summoner/{Uri.EscapeDataString(spectatorIdentity)}";
        using var activeResponse = await SendGetAsync(
            activePath,
            "active_game",
            cancellationToken,
            options.GetPlatformBaseUri())
            .ConfigureAwait(false);
        if (activeResponse.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!activeResponse.IsSuccessStatusCode)
        {
            throw new RiotApiException(activeResponse.StatusCode);
        }

        var payload = await activeResponse.Content.ReadFromJsonAsync<ActiveGameResponse>(
            cancellationToken: cancellationToken).ConfigureAwait(false);
        if (payload is null || payload.GameId <= 0 || payload.GameStartTime <= 0 || payload.Participants?.Count > 10)
        {
            throw new RiotApiException(HttpStatusCode.BadGateway);
        }

        var participants = await EnrichActiveGameParticipantsAsync(
                payload.Participants ?? [],
                cancellationToken)
            .ConfigureAwait(false);

        return new RiotActiveGame(
            payload.GameId,
            payload.GameQueueConfigId,
            DateTimeOffset.FromUnixTimeMilliseconds(payload.GameStartTime),
            Math.Max(0, payload.GameLength),
            participants);
    }

    private async Task<IReadOnlyList<RiotActiveGameParticipant>> EnrichActiveGameParticipantsAsync(
        IReadOnlyList<ActiveGameParticipantResponse> participants,
        CancellationToken cancellationToken)
    {
        var enriched = new List<RiotActiveGameParticipant>(participants.Count);
        foreach (var participant in participants)
        {
            var puuid = participant.Puuid;
            var gameName = participant.SummonerName;
            string? tagLine = null;
            var summonerId = participant.SummonerId;

            if (!string.IsNullOrWhiteSpace(summonerId))
            {
                var summonerPath = $"lol/summoner/v4/summoners/by-summoner/{Uri.EscapeDataString(summonerId)}";
                using var summonerResponse = await SendGetAsync(
                    summonerPath,
                    "active_game_participant",
                    cancellationToken,
                    options.GetPlatformBaseUri()).ConfigureAwait(false);
                if (summonerResponse.IsSuccessStatusCode)
                {
                    var summoner = await summonerResponse.Content.ReadFromJsonAsync<SummonerResponse>(
                        cancellationToken: cancellationToken).ConfigureAwait(false);
                    puuid ??= summoner?.Puuid;
                    gameName ??= summoner?.Name;
                }
            }

            if (!string.IsNullOrWhiteSpace(puuid))
            {
                var accountPath = $"riot/account/v1/accounts/by-puuid/{Uri.EscapeDataString(puuid)}";
                using var accountResponse = await SendGetAsync(
                    accountPath,
                    "active_game_participant_account",
                    cancellationToken).ConfigureAwait(false);
                if (accountResponse.IsSuccessStatusCode)
                {
                    var account = await accountResponse.Content.ReadFromJsonAsync<AccountResponse>(
                        cancellationToken: cancellationToken).ConfigureAwait(false);
                    gameName = account?.GameName ?? gameName;
                    tagLine = account?.TagLine;
                }
            }

            enriched.Add(new RiotActiveGameParticipant(
                puuid,
                gameName,
                tagLine,
                summonerId,
                participant.ChampionId,
                participant.TeamId));
        }

        return enriched;
    }

    private async Task<HttpResponseMessage> SendGetAsync(
        string path,
        string endpoint,
        CancellationToken cancellationToken,
        Uri? baseUri = null)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new InvalidOperationException("RIOT_API_KEY must be configured at runtime before calling Riot.");
        }

        var uri = new Uri(baseUri ?? options.GetRegionalBaseUri(), path);
        try
        {
            for (var attempt = 0; ; attempt++)
            {
                HttpResponseMessage response;
                TimeSpan retryAfter = default;
                using (await rateLimiter.AcquireAsync(uri.Host, endpoint, cancellationToken).ConfigureAwait(false))
                {
                    using var request = new HttpRequestMessage(HttpMethod.Get, uri);
                    request.Headers.TryAddWithoutValidation("X-Riot-Token", options.ApiKey);
                    var started = Stopwatch.GetTimestamp();
                    try
                    {
                        response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception error) when (error is HttpRequestException or OperationCanceledException)
                    {
                        metrics.RecordRiotRequest(endpoint, 0);
                        throw;
                    }
                    finally
                    {
                        metrics.RecordStage("riot.http", Stopwatch.GetElapsedTime(started));
                    }
                    metrics.RecordRiotRequest(endpoint, (int)response.StatusCode);
                    try
                    {
                        rateLimiter.ObserveLimits(uri.Host, endpoint,
                            Header(response, "X-App-Rate-Limit"), Header(response, "X-App-Rate-Limit-Count"),
                            Header(response, "X-Method-Rate-Limit"), Header(response, "X-Method-Rate-Limit-Count"));
                        if (response.StatusCode == HttpStatusCode.TooManyRequests)
                        {
                            retryAfter = GetRetryDelay(response, attempt);
                            // Publish cooldown before releasing the concurrency permit.
                            rateLimiter.RegisterRetryAfter(retryAfter);
                        }
                    }
                    catch { response.Dispose(); throw; }
                }

                if (response.StatusCode != HttpStatusCode.TooManyRequests
                    || attempt >= options.MaxRetryAttempts || retryAfter > TimeSpan.FromSeconds(options.MaxRetryDelaySeconds)) return response;
                metrics.RecordRiotRetry();
                response.Dispose();
            }
        }
        catch (OperationCanceledException)
        {
            metrics.RecordRiotCancellation();
            throw;
        }
    }

    private static string? Header(HttpResponseMessage response, string name) =>
        response.Headers.TryGetValues(name, out var values) ? string.Join(',', values) : null;

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

        if (participants.Length is 0 or > 10 || participants.Select(participant => participant.Puuid).Distinct(StringComparer.Ordinal).Count() != participants.Length)
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

    private sealed record SummonerResponse(
        [property: JsonPropertyName("id")] string? Id,
        [property: JsonPropertyName("puuid")] string? Puuid,
        [property: JsonPropertyName("name")] string? Name);

    private sealed record ActiveGameResponse(
        [property: JsonPropertyName("gameId")] long GameId,
        [property: JsonPropertyName("gameStartTime")] long GameStartTime,
        [property: JsonPropertyName("gameLength")] int GameLength,
        [property: JsonPropertyName("gameQueueConfigId")] int GameQueueConfigId,
        [property: JsonPropertyName("participants")] IReadOnlyList<ActiveGameParticipantResponse>? Participants);

    private sealed record ActiveGameParticipantResponse(
        [property: JsonPropertyName("puuid")] string? Puuid,
        [property: JsonPropertyName("summonerName")] string? SummonerName,
        [property: JsonPropertyName("summonerId")] string? SummonerId,
        [property: JsonPropertyName("championId")] int ChampionId,
        [property: JsonPropertyName("teamId")] int TeamId);
}
