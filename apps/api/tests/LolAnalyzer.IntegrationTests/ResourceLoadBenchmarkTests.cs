using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using LolAnalyzer.Application.Caching;
using LolAnalyzer.Application.Observability;
using LolAnalyzer.Application.Riot;
using LolAnalyzer.Infrastructure.Riot;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace LolAnalyzer.IntegrationTests;

/// <summary>
/// Opt-in, in-process TestServer benchmark. This exact file can be copied into the T03.5
/// checkout for comparison; it deliberately uses only APIs present in that baseline.
/// No sockets, live Riot requests, production services or mandatory dependencies are used.
/// </summary>
[CollectionDefinition("resource-load", DisableParallelization = true)]
public sealed class ResourceLoadBenchmarkSuite;

[Collection("resource-load")]
public sealed class ResourceLoadBenchmarkTests(ITestOutputHelper output)
{
    private static readonly JsonSerializerOptions OutputJsonOptions = new() { WriteIndented = true };
    public static bool IsLoadEnabled => Environment.GetEnvironmentVariable("RUN_RESOURCE_LOAD") == "1";

    [Fact(Skip = "Opt-in isolated load only: RUN_RESOURCE_LOAD=1", SkipUnless = nameof(IsLoadEnabled))]
    public async Task IsolatedSimulatedHttpLoad()
    {
        var users = ReadNumber("RESOURCE_LOAD_USERS", 1, 1, 100);
        var waves = ReadNumber("RESOURCE_LOAD_WAVES", 3, 1, 10);
        var delay = ReadNumber("RESOURCE_LOAD_DELAY_MS", 5, 1, 100);
        var warm = Environment.GetEnvironmentVariable("RESOURCE_LOAD_CACHE") == "warm";
        var distinct = Environment.GetEnvironmentVariable("RESOURCE_LOAD_IDENTITIES") == "distinct";
        var analysis = Environment.GetEnvironmentVariable("RESOURCE_LOAD_FLOW") != "search";
        var sharedClient = Environment.GetEnvironmentVariable("RESOURCE_LOAD_CLIENTS") == "shared";
        var label = Environment.GetEnvironmentVariable("RESOURCE_LOAD_LABEL") ?? "working-tree";
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        deadline.CancelAfter(TimeSpan.FromMinutes(4));
        await using var factory = new LoadFactory(delay, sharedClient);
        using var client = factory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(200);
        var cache = factory.Services.GetRequiredService<ICacheService>();
        var metrics = factory.Services.GetRequiredService<OperationalMetrics>();

        // Prime JIT/application paths, then discard only the optional Riot response cache.
        await SendAsync(client, 0, analysis, deadline.Token, warming: true);
        await ClearCacheAsync(cache, deadline.Token);
        if (warm)
            for (var index = 0; index < (distinct ? users : 1); index++)
                await SendAsync(client, index, analysis, deadline.Token, index, warming: true);

        FullCollection();
        using var process = Process.GetCurrentProcess();
        process.Refresh();
        var initialRss = process.WorkingSet64;
        var initialHeap = GC.GetTotalMemory(false);
        var cpuBefore = process.TotalProcessorTime;
        var allocationBefore = GC.GetTotalAllocatedBytes();
        var cacheBefore = metrics.Snapshot();
        var callsBefore = factory.Handler.Calls;
        var peakRss = initialRss;
        using var sampling = new CancellationTokenSource();
        var rssGuardBytes = ReadNumber("RESOURCE_LOAD_RSS_GUARD_MIB", 384, 128, 1024) * 1024L * 1024L;
        var sampler = SampleWorkingSetAsync(process, value =>
        {
            peakRss = Math.Max(peakRss, value);
            if (value > rssGuardBytes) deadline.Cancel();
        }, sampling.Token);
        var rows = new List<Sample>(users * waves);
        var elapsed = Stopwatch.StartNew();
        try
        {
            for (var wave = 0; wave < waves; wave++)
            {
                if (!warm) await ClearCacheAsync(cache, deadline.Token);
                var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                var pending = Enumerable.Range(0, users).Select(async index =>
                {
                    await start.Task;
                    return await SendAsync(client, distinct ? index : 0, analysis, deadline.Token, index);
                }).ToArray();
                start.SetResult();
                rows.AddRange(await Task.WhenAll(pending));
            }
        }
        finally
        {
            elapsed.Stop();
            sampling.Cancel();
            await sampler;
        }
        process.Refresh();
        peakRss = Math.Max(peakRss, process.WorkingSet64);
        var cpuMilliseconds = (process.TotalProcessorTime - cpuBefore).TotalMilliseconds;
        var allocated = GC.GetTotalAllocatedBytes() - allocationBefore;
        var after = metrics.Snapshot();
        var calls = factory.Handler.Calls - callsBefore;
        var hits = after.CacheHits - cacheBefore.CacheHits;
        var misses = after.CacheMisses - cacheBefore.CacheMisses;
        FullCollection();
        process.Refresh();
        var successful = rows.Where(row => row.Status == 200).ToArray();
        var rejected = rows.Count(row => row.Status is 429 or 503);
        var errors = rows.Count - successful.Length - rejected;
        var result = new
        {
            schemaVersion = 1,
            label,
            runtime = Environment.Version.ToString(),
            architecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString(),
            transport = "ASP.NET TestServer; real RiotApiClient + simulated HttpMessageHandler; no network",
            users,
            waves,
            offeredRequests = rows.Count,
            flow = analysis ? "analysis" : "search",
            cache = warm ? "warm" : "cold-at-start-of-each-wave",
            identities = distinct ? "distinct" : "same",
            clientAddresses = sharedClient ? "shared-test-client" : "one-test-address-per-virtual-user",
            simulatedCallDelayMilliseconds = delay,
            rssGuardBytes,
            completed = successful.Length,
            rejected,
            errors,
            statusCounts = rows.GroupBy(row => row.Status).ToDictionary(group => group.Key.ToString(CultureInfo.InvariantCulture), group => group.Count()),
            successLatencyMilliseconds = Percentiles(successful.Select(row => row.DurationMilliseconds)),
            allResponseLatencyMilliseconds = Percentiles(rows.Select(row => row.DurationMilliseconds)),
            elapsedMilliseconds = elapsed.Elapsed.TotalMilliseconds,
            completedRequestsPerSecond = successful.Length / elapsed.Elapsed.TotalSeconds,
            allResponsesPerSecond = rows.Count / elapsed.Elapsed.TotalSeconds,
            initialWorkingSetBytes = initialRss,
            sampledPeakWorkingSetBytes = peakRss,
            workingSetAfterFullGcBytes = process.WorkingSet64,
            initialManagedHeapBytes = initialHeap,
            managedHeapAfterFullGcBytes = GC.GetTotalMemory(false),
            managedHeapCommittedAfterFullGcBytes = GC.GetGCMemoryInfo().TotalCommittedBytes,
            accumulatedAllocatedBytes = allocated,
            processCpuMilliseconds = cpuMilliseconds,
            simulatedExternalCalls = calls,
            cacheHits = hits,
            cacheMisses = misses,
            cacheHitRatio = hits + misses == 0 ? (double?)null : (double)hits / (hits + misses),
            notes = "Working set is process-wide and sampled every 5 ms, not per-request RAM. Allocated bytes are accumulated allocations, not retained memory. Full GC occurs only in this opt-in benchmark. p99 emitted only for >=100 samples. Rejections may differ under scheduling."
        };
        var json = JsonSerializer.Serialize(result, OutputJsonOptions);
        output.WriteLine("RESOURCE_LOAD_RESULT " + json);
        var destination = Environment.GetEnvironmentVariable("RESOURCE_LOAD_RESULT_PATH");
        if (!string.IsNullOrEmpty(destination)) await File.WriteAllTextAsync(destination, json, deadline.Token);
        Assert.Equal(0, errors);
        Assert.Equal(rows.Count, successful.Length + rejected);
        Assert.True(successful.Length > 0, "At least one request must complete; rejections alone are not a throughput success.");
    }

    private static async Task<Sample> SendAsync(HttpClient client, int identity, bool analysis, CancellationToken token, int clientIndex = 0, bool warming = false)
    {
        var watch = Stopwatch.StartNew();
        using var request = new HttpRequestMessage(analysis ? HttpMethod.Post : HttpMethod.Get, analysis
            ? $"/api/v1/matches/LA1_G{identity}TARGET/analyze"
            : $"/api/v1/players/by-riot-id/player-{identity}/TEST/analysis?platformRegion=LA1");
        request.Headers.Add("X-Load-Test-Client", clientIndex.ToString(CultureInfo.InvariantCulture));
        if (warming) request.Headers.Add("X-Load-Test-Warmup", "1");
        if (analysis) request.Content = JsonContent.Create(new { ownerPuuid = $"owner-{identity}", platformRegion = "LA1" });
        using var response = await client.SendAsync(request, token);
        // Include transfer + JSON parsing in observed HTTP latency and assert semantic completeness.
        if (response.StatusCode == HttpStatusCode.OK)
        {
            using var json = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(token));
            Assert.Equal("complete", json.RootElement.GetProperty("coverage").GetProperty("status").GetString());
            if (!analysis) Assert.Equal(5, json.RootElement.GetProperty("matches").GetProperty("items").GetArrayLength());
        }
        else await response.Content.CopyToAsync(Stream.Null, token);
        return new Sample((int)response.StatusCode, watch.Elapsed.TotalMilliseconds);
    }

    private static object Percentiles(IEnumerable<double> durations)
    {
        var sorted = durations.Order().ToArray();
        double? At(double percentile) => sorted.Length == 0 ? null : sorted[(int)Math.Ceiling(sorted.Length * percentile) - 1];
        return new { samples = sorted.Length, p50 = At(.50), p95 = At(.95), p99 = sorted.Length >= 100 ? At(.99) : null };
    }

    private static int ReadNumber(string name, int fallback, int minimum, int maximum)
    {
        var raw = Environment.GetEnvironmentVariable(name);
        var value = string.IsNullOrEmpty(raw) ? fallback : int.Parse(raw, CultureInfo.InvariantCulture);
        if (value < minimum || value > maximum) throw new ArgumentOutOfRangeException(name);
        return value;
    }

    private static async Task ClearCacheAsync(ICacheService cache, CancellationToken token)
    {
        await cache.RemoveTagAsync("riot-identity", token);
        await cache.RemoveTagAsync("riot-match-ids", token);
        await cache.RemoveTagAsync("riot-match-details", token);
    }

    private static void FullCollection()
    {
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
    }

    private static async Task SampleWorkingSetAsync(Process process, Action<long> sample, CancellationToken token)
    {
        try
        {
            while (true)
            {
                process.Refresh();
                sample(process.WorkingSet64);
                await Task.Delay(5, token);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
    }

    private sealed record Sample(int Status, double DurationMilliseconds);

    private sealed class LoadFactory(int delay, bool sharedClient) : WebApplicationFactory<Program>
    {
        public SimulatedRiotHandler Handler { get; } = new(delay);
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureLogging(logging => logging.ClearProviders());
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IStartupFilter>(new TestClientAddresses(sharedClient));
                // Throughput experiment isolates HTTP admission + cache + concurrency;
                // a separate resilience test exercises real configured Riot quotas.
                // Reflection keeps this identical source compatible with T03.5 options.
                services.RemoveAll<IRiotRateLimiter>();
                services.AddSingleton<IRiotRateLimiter>(_ =>
                {
                    var options = new RiotOptions();
                    typeof(RiotOptions).GetProperty("ShortWindowRequests")?.SetValue(options, 10000);
                    typeof(RiotOptions).GetProperty("LongWindowRequests")?.SetValue(options, 10000);
                    typeof(RiotOptions).GetProperty("ShortWindowSeconds")?.SetValue(options, 1);
                    typeof(RiotOptions).GetProperty("LongWindowSeconds")?.SetValue(options, 1);
                    return new RiotRateLimiter(options, TimeProvider.System);
                });
                services.RemoveAll<IRiotApiClient>();
                services.AddSingleton(_ => new HttpClient(Handler)
                { Timeout = TimeSpan.FromSeconds(10), MaxResponseContentBufferSize = 1024 * 1024 });
                services.AddSingleton<IRiotApiClient>(provider => new RiotApiClient(
                    provider.GetRequiredService<HttpClient>(),
                    new RiotOptions { ApiKey = "simulated-test-key", PlatformRegion = "la1", RegionalRouting = "americas" },
                    provider.GetRequiredService<IRiotRateLimiter>(), provider.GetRequiredService<TimeProvider>(),
                    provider.GetRequiredService<OperationalMetrics>(), provider.GetRequiredService<IRiotResponseCache>(),
                    provider.GetRequiredService<CacheOptions>()));
            });
        }
    }

    // TestServer has no real network peer. This test-only filter sets Connection.RemoteIpAddress;
    // production never consumes this header and must never copy this trust mechanism.
    private sealed class TestClientAddresses(bool sharedClient) : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use(async (context, continuation) =>
            {
                var warming = context.Request.Headers["X-Load-Test-Warmup"] == "1";
                var index = sharedClient && !warming ? 0 : int.Parse(context.Request.Headers["X-Load-Test-Client"].ToString(), CultureInfo.InvariantCulture);
                context.Connection.RemoteIpAddress = new IPAddress(new byte[] { 192, 0, warming ? (byte)3 : (byte)2, checked((byte)(index + 1)) });
                await continuation(context);
            });
            next(app);
        };
    }

    private sealed class SimulatedRiotHandler(int delay) : HttpMessageHandler
    {
        private int calls;
        private static readonly DateTimeOffset TargetTime = DateTimeOffset.UnixEpoch.AddDays(20_000);
        public int Calls => Volatile.Read(ref calls);
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref calls);
            await Task.Delay(delay, cancellationToken);
            var uri = request.RequestUri ?? throw new InvalidOperationException("Missing simulated request URI.");
            var path = Uri.UnescapeDataString(uri.AbsolutePath);
            object payload;
            if (path.Contains("/accounts/by-riot-id/", StringComparison.Ordinal))
            {
                var name = path.Split('/')[^2];
                var identity = name["player-".Length..];
                payload = new { puuid = $"owner-{identity}", gameName = name, tagLine = "TEST" };
            }
            else if (path.EndsWith("/ids", StringComparison.Ordinal))
            {
                var puuid = path.Split('/')[^2];
                var identity = puuid[(puuid.LastIndexOf('-') + 1)..];
                var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
                var count = int.Parse(query["count"] ?? "5", CultureInfo.InvariantCulture);
                payload = Enumerable.Range(0, count).Select(index => index == 0
                    ? $"LA1_G{identity}TARGET" : $"LA1_G{identity}H{index}").ToArray();
            }
            else
            {
                var id = path.Split('/')[^1];
                var suffix = id[5..];
                var identity = suffix[..suffix.IndexOfAny(['T', 'H'])];
                var target = id.EndsWith("TARGET", StringComparison.Ordinal);
                var number = target ? 0 : int.Parse(id[(id.LastIndexOf('H') + 1)..], CultureInfo.InvariantCulture);
                var time = TargetTime.AddDays(-number);
                payload = new
                {
                    metadata = new { matchId = id },
                    info = new
                    {
                        queueId = 420,
                        gameCreation = time.ToUnixTimeMilliseconds(),
                        gameStartTimestamp = time.ToUnixTimeMilliseconds(),
                        gameEndTimestamp = time.AddMinutes(30).ToUnixTimeMilliseconds(),
                        gameDuration = 1800,
                        gameVersion = "fixture",
                        participants = Enumerable.Range(0, 10).Select(index => new
                        {
                            puuid = index == 0 ? $"owner-{identity}" : $"p{index}-{identity}",
                            riotIdGameName = index == 0 ? $"player-{identity}" : $"p{index}-{identity}",
                            riotIdTagline = "TEST",
                            teamId = index < 5 ? 100 : 200,
                            participantId = index + 1,
                            championId = index + 1,
                            championName = "FixtureChampion",
                            teamPosition = "MIDDLE",
                            individualPosition = "MIDDLE",
                            kills = 3,
                            deaths = 2,
                            assists = 5,
                            win = index < 5,
                            goldEarned = 10000,
                            totalDamageDealtToChampions = 10000,
                            visionScore = 10,
                            totalMinionsKilled = 100,
                            neutralMinionsKilled = 0
                        }).ToArray()
                    }
                };
            }
            return new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json") };
        }
    }
}
