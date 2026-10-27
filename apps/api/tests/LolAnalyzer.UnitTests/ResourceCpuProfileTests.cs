using System.Diagnostics;
using System.Text.Json;
using LolAnalyzer.Application.Stateless;
using LolAnalyzer.Infrastructure.Caching;
using LolAnalyzer.Testing;
using Xunit;

namespace LolAnalyzer.UnitTests;

[CollectionDefinition("resource-cpu-profile", DisableParallelization = true)]
public sealed class ResourceCpuProfileSuite;

[Collection("resource-cpu-profile")]
public sealed class ResourceCpuProfileTests(ITestOutputHelper output)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static bool IsProfileEnabled => Environment.GetEnvironmentVariable("RUN_RESOURCE_CPU_PROFILE") == "1";

    [Fact(Skip = "Opt-in CPU profile only: RUN_RESOURCE_CPU_PROFILE=1", SkipUnless = nameof(IsProfileEnabled))]
    public async Task ProfileBoundedSyntheticStagesWithoutExternalTraffic()
    {
        var token = TestContext.Current.CancellationToken;
        var target = StatelessFixture.Match("LA1_TARGET");
        var history = Enumerable.Range(1, 5).Select(index => StatelessFixture.Match($"LA1_H{index}")).ToArray();
        var coverage = new AnalysisCoverage("complete", 10, 32, 5, 5, [], []);
        var calculator = StatelessFixture.Calculator();
        var result = calculator.Analyze(target, "owner", "LA1", history, coverage, token);
        var json = JsonSerializer.Serialize(history);
        var cache = new MemoryCacheService(TimeProvider.System);
        var measurements = new List<Stage>();
        measurements.Add(await MeasureAsync("normalized-history-json-dom-parse", () =>
        { using var document = JsonDocument.Parse(json); return Task.CompletedTask; }));
        measurements.Add(await MeasureAsync("relationships-familiarity-premades-summary", () =>
        { _ = calculator.Analyze(target, "owner", "LA1", history, coverage, token); return Task.CompletedTask; }));
        measurements.Add(await MeasureAsync("response-json-serialization", () =>
        { _ = JsonSerializer.Serialize(result); return Task.CompletedTask; }));
        measurements.Add(await MeasureAsync("cache-entry-estimation-and-replacement", () =>
            cache.SetAsync("fixed-key", history, TimeSpan.FromMinutes(1), "profile", token)));
        output.WriteLine(JsonSerializer.Serialize(new
        {
            runtimeAvailableHeapBytes = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes,
            stages = measurements
        }, JsonOptions));
        GC.KeepAlive(cache);
    }

    private static async Task<Stage> MeasureAsync(string name, Func<Task> action)
    {
        const int iterations = 100;
        for (var index = 0; index < 5; index++) await action();
        GC.Collect();
        using var process = Process.GetCurrentProcess();
        var cpu = process.TotalProcessorTime;
        var allocated = GC.GetTotalAllocatedBytes();
        var start = Stopwatch.GetTimestamp();
        for (var index = 0; index < iterations; index++) await action();
        return new Stage(name, iterations, Stopwatch.GetElapsedTime(start).TotalMilliseconds,
            (process.TotalProcessorTime - cpu).TotalMilliseconds, GC.GetTotalAllocatedBytes() - allocated);
    }

    private sealed record Stage(string Name, int Iterations, double WallMilliseconds, double ProcessCpuMilliseconds, long AllocatedBytes);
}
