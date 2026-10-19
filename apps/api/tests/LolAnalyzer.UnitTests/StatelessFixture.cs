using System.Collections.Concurrent;
using LolAnalyzer.Application.Analysis;
using LolAnalyzer.Application.Caching;
using LolAnalyzer.Application.Observability;
using LolAnalyzer.Application.Riot;
using LolAnalyzer.Application.Stateless;
using LolAnalyzer.Infrastructure.Caching;
using LolAnalyzer.Infrastructure.Riot;

namespace LolAnalyzer.Testing;

public sealed class StatelessFixture : IRiotApiClient
{
    private static readonly string[] TargetIds = ["LA1_TARGET"];
    private static readonly string[] FutureIds = ["LA1_FUTURE"];
    private int accountCalls;
    public static DateTimeOffset TargetTime { get; } = DateTimeOffset.UnixEpoch.AddDays(20_000);
    public int DelayMilliseconds { get; set; }
    public bool SeparateHistories { get; set; }
    public bool InjectFuture { get; set; }
    public int HistoryLength { get; set; } = 5;
    public string? FailingPuuid { get; set; }
    public string? FailingMatch { get; set; }
    public bool RateLimited { get; set; }
    public ConcurrentQueue<(string Puuid, int Count, long? EndTime)> Lists { get; } = new();
    public ConcurrentDictionary<string, int> Details { get; } = new(StringComparer.Ordinal);
    public int AccountCalls => Volatile.Read(ref accountCalls);

    public async Task<RiotAccount?> GetAccountByRiotIdAsync(string gameName, string tagLine, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref accountCalls);
        await DelayAsync(cancellationToken);
        return gameName == "missing" ? null : new RiotAccount("owner", gameName, tagLine);
    }

    public async Task<IReadOnlyList<string>> GetMatchIdsAsync(string puuid, int start, int count, CancellationToken cancellationToken)
    {
        Lists.Enqueue((puuid, count, null));
        await DelayAsync(cancellationToken);
        if (puuid == FailingPuuid) throw new RiotApiException(System.Net.HttpStatusCode.ServiceUnavailable);
        return TargetIds.Concat(Enumerable.Range(1, HistoryLength).Select(index => $"LA1_H{index}"))
            .Skip(start).Take(count).ToArray();
    }

    public async Task<IReadOnlyList<string>> GetMatchIdsBeforeAsync(string puuid, int count, long endTime, CancellationToken cancellationToken)
    {
        Lists.Enqueue((puuid, count, endTime));
        await DelayAsync(cancellationToken);
        if (puuid == FailingPuuid || RateLimited) throw new RiotApiException(RateLimited
            ? System.Net.HttpStatusCode.TooManyRequests : System.Net.HttpStatusCode.ServiceUnavailable);
        var prefix = puuid.StartsWith("alt", StringComparison.Ordinal) ? "LA1_ALT_H"
            : SeparateHistories ? $"LA1_{puuid}_H" : "LA1_H";
        var ids = Enumerable.Range(1, HistoryLength).Select(index => $"{prefix}{index}");
        if (InjectFuture) ids = FutureIds.Concat(ids);
        return TargetIds.Concat(ids).Take(count).ToArray();
    }

    public async Task<RiotMatchData?> GetMatchAsync(string riotMatchId, CancellationToken cancellationToken)
    {
        Details.AddOrUpdate(riotMatchId, 1, (_, previous) => previous + 1);
        await DelayAsync(cancellationToken);
        if (riotMatchId == FailingMatch) throw new RiotApiException(System.Net.HttpStatusCode.ServiceUnavailable);
        if (riotMatchId == "missing") return null;
        return Match(riotMatchId);
    }

    public static RiotMatchData Match(string id)
    {
        var alt = id.Contains("ALT", StringComparison.Ordinal);
        var target = id is "LA1_TARGET" or "LA1_ALT";
        var future = id == "LA1_FUTURE";
        var number = int.TryParse(id.AsSpan(id.LastIndexOf('H') + 1), out var parsed) ? parsed : 1;
        var time = target ? TargetTime : future ? TargetTime.AddDays(1) : TargetTime.AddDays(-number);
        var specific = id.StartsWith("LA1_owner_", StringComparison.Ordinal) ? "owner"
            : id.StartsWith("LA1_p", StringComparison.Ordinal) ? id.Split('_')[1] : null;
        var participants = Enumerable.Range(0, 10).Select(index =>
        {
            var puuid = specific is not null
                ? index == 0 ? specific : $"outsider-{id}-{index}"
                : alt ? index == 0 ? "alt-owner" : $"alt-p{index}"
                : index == 0 ? "owner" : $"p{index}";
            return new RiotMatchParticipantData(puuid, puuid == "owner" ? "Fixture User" : puuid, "TEST", index < 5 ? 100 : 200, index + 1,
                index + 1, "TestChampion", null, null, index, 2, 3, index < 5 ? number % 2 == 1 : number % 2 == 0,
                0, 0, 0, 0);
        }).ToArray();
        return new RiotMatchData(id, 420, time, time, time.AddMinutes(30), 1800, "test", "{}", participants);
    }

    private Task DelayAsync(CancellationToken cancellationToken) => Task.Delay(DelayMilliseconds, cancellationToken);

    public static OnDemandAnalysisService Service(IRiotApiClient riot) => new(riot, Calculator(),
        new OnDemandAnalysisOptions(), new OperationalMetrics());

    public static RequestAnalysisCalculator Calculator() => new(
        new PlayerRelationshipAnalyzer(new RelationshipScoreCalculator(new RelationshipScoreOptions())),
        new PossiblePremadeDetector(new PremadeDetectionOptions()), new PossiblePremadeGroupDetector(new PremadeGroupDetectionOptions()));
}

public sealed class CachedFixtureClient(IRiotApiClient inner, IRiotResponseCache cache) : IRiotApiClient
{
    public Task<RiotAccount?> GetAccountByRiotIdAsync(string gameName, string tagLine, CancellationToken cancellationToken) =>
        cache.GetOrCreateAsync($"identity:{gameName}:{tagLine}", TimeSpan.FromMinutes(30), "identities",
            token => inner.GetAccountByRiotIdAsync(gameName, tagLine, token), cancellationToken);

    public async Task<IReadOnlyList<string>> GetMatchIdsAsync(string puuid, int start, int count, CancellationToken cancellationToken) =>
        await cache.GetOrCreateAsync($"ids:{puuid}:{start}:{count}", TimeSpan.FromMinutes(2), "ids",
            async token => (await inner.GetMatchIdsAsync(puuid, start, count, token)).ToArray(), cancellationToken) ?? [];

    public async Task<IReadOnlyList<string>> GetMatchIdsBeforeAsync(string puuid, int count, long endTime, CancellationToken cancellationToken) =>
        await cache.GetOrCreateAsync($"ids:{puuid}:{count}:{endTime}", TimeSpan.FromMinutes(2), "ids",
            async token => (await inner.GetMatchIdsBeforeAsync(puuid, count, endTime, token)).ToArray(), cancellationToken) ?? [];

    public Task<RiotMatchData?> GetMatchAsync(string riotMatchId, CancellationToken cancellationToken) =>
        cache.GetOrCreateAsync($"match:{riotMatchId}", TimeSpan.FromMinutes(60), "matches", token => inner.GetMatchAsync(riotMatchId, token), cancellationToken);
}

public sealed class AdjustableTimeProvider : TimeProvider
{
    private DateTimeOffset now = DateTimeOffset.UtcNow;
    public override DateTimeOffset GetUtcNow() => now;
    public void Advance(TimeSpan duration) => now += duration;
}
