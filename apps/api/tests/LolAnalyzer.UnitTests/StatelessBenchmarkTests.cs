using System.Diagnostics;
using System.Text.Json;
using LolAnalyzer.Application.Analysis;
using LolAnalyzer.Application.Caching;
using LolAnalyzer.Application.Matches;
using LolAnalyzer.Application.Observability;
using LolAnalyzer.Application.Players;
using LolAnalyzer.Application.Riot;
using LolAnalyzer.Application.Stateless;
using LolAnalyzer.Infrastructure.Caching;
using LolAnalyzer.Infrastructure.Persistence;
using LolAnalyzer.Infrastructure.Riot;
using LolAnalyzer.Testing;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LolAnalyzer.UnitTests;

[CollectionDefinition("stateless-benchmark", DisableParallelization = true)]
public sealed class StatelessBenchmarkSuite;

[Collection("stateless-benchmark")]
public sealed class StatelessBenchmarkTests(ITestOutputHelper output)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    public static bool IsBenchmarkEnabled => Environment.GetEnvironmentVariable("RUN_STATELESS_BENCHMARK") == "1";

    [Fact(Skip = "Opt-in T03.5 comparison only: RUN_STATELESS_BENCHMARK=1", SkipUnless = nameof(IsBenchmarkEnabled))]
    public async Task CompareLegacyT03AndStatelessPipelinesWithSimulatedRiot()
    {
        // Opt-in avoids turning a performance experiment into an ordinary CI assertion.
        const int samples = 12;
        var rows = new List<Measurement>();
        foreach (var scenario in new[] { "search", "selected-match" })
            foreach (var architecture in new[] { "legacy-t03", "stateless" })
                for (var index = 0; index <= samples; index++)
                {
                    var external = new StatelessFixture { DelayMilliseconds = 3 };
                    using var metrics = new OperationalMetrics();
                    var cache = new MemoryCacheService(TimeProvider.System);
                    var client = new CachedFixtureClient(external, new RiotResponseCache(cache, metrics));
                    var database = new DbContextOptionsBuilder<LolAnalyzerDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
                    var service = new OnDemandAnalysisService(client, StatelessFixture.Calculator(), new OnDemandAnalysisOptions(), metrics);
                    var legacy = new LegacyPipeline(client, database, metrics);
                    foreach (var temperature in new[] { "cold", "warm" })
                    {
                        var callsBefore = Calls(external);
                        var hitsBefore = metrics.Snapshot().CacheHits;
                        var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
                        var cpuBefore = Process.GetCurrentProcess().TotalProcessorTime;
                        var start = Stopwatch.GetTimestamp();
                        if (architecture == "legacy-t03")
                        {
                            if (scenario == "search") await legacy.SearchAsync();
                            else
                            {
                                // T03 cannot analyze a selected match from an empty DB: bootstrap is required.
                                if (temperature == "cold") await legacy.SearchAsync();
                                await legacy.MatchAsync();
                            }
                        }
                        else if (scenario == "search")
                            Assert.Equal(5, (await service.SearchAsync("Fixture User", "TEST", "LA1", TestContext.Current.CancellationToken))!.Matches.Items.Count);
                        else
                            Assert.Equal(9, (await service.AnalyzeAsync("LA1_TARGET", "owner", "LA1", TestContext.Current.CancellationToken))!.Match.Familiarity!.KnownPlayers);
                        if (index > 0) rows.Add(new Measurement(architecture, scenario, temperature, Stopwatch.GetElapsedTime(start).TotalMilliseconds,
                            (Process.GetCurrentProcess().TotalProcessorTime - cpuBefore).TotalMilliseconds,
                            GC.GetTotalAllocatedBytes(precise: true) - allocatedBefore,
                            Calls(external) - callsBefore, metrics.Snapshot().CacheHits - hitsBefore));
                    }
                    await using var cleanup = new LolAnalyzerDbContext(database);
                    await cleanup.Database.EnsureDeletedAsync(TestContext.Current.CancellationToken);
                }
        var summary = rows.GroupBy(row => (row.Architecture, row.Scenario, row.Temperature)).Select(group => new
        {
            group.Key.Architecture,
            group.Key.Scenario,
            group.Key.Temperature,
            Samples = group.Count(),
            P50Milliseconds = Percentile(group.Select(row => row.Milliseconds), 0.50),
            P95Milliseconds = Percentile(group.Select(row => row.Milliseconds), 0.95),
            MeanCpuMilliseconds = Math.Round(group.Average(row => row.CpuMilliseconds), 2),
            MeanAllocatedBytes = (long)group.Average(row => row.AllocatedBytes),
            MeanSimulatedExternalCalls = group.Average(row => row.Calls),
            MeanCacheHits = group.Average(row => row.Hits),
        }).ToArray();
        output.WriteLine(JsonSerializer.Serialize(summary, JsonOptions));
    }

    private static int Calls(StatelessFixture fixture) => fixture.AccountCalls + fixture.Lists.Count + fixture.Details.Values.Sum();
    private static double Percentile(IEnumerable<double> samples, double percentile)
    {
        var sorted = samples.Order().ToArray();
        return Math.Round(sorted[(int)Math.Ceiling(sorted.Length * percentile) - 1], 2);
    }

    private sealed class LegacyPipeline(
        IRiotApiClient client,
        DbContextOptions<LolAnalyzerDbContext> options,
        OperationalMetrics metrics)
    {
        private readonly PlayerRelationshipAnalyzer relationshipAnalyzer = new(new RelationshipScoreCalculator(new RelationshipScoreOptions()));
        private readonly PossiblePremadeDetector pairDetector = new(new PremadeDetectionOptions());
        private readonly PossiblePremadeGroupDetector groupDetector = new(new PremadeGroupDetectionOptions());
        private readonly PlayerRelationshipAnalysisOptions relationshipOptions = new();
        private readonly MatchIngestionOptions ingestionOptions = new();

        public async Task SearchAsync()
        {
            await using var db = new LolAnalyzerDbContext(options);
            var lookup = new PlayerLookupService(client, new PlayerRepository(db));
            await lookup.FindByRiotIdAsync("Fixture User", "TEST", "LA1", TestContext.Current.CancellationToken);
            await SyncAsync(db, "owner", 5, null);
            Assert.Equal(5, (await new PlayerAnalysisRepository(db).GetSummaryAsync("owner", TestContext.Current.CancellationToken))!.MatchesAnalyzed);
        }

        public async Task MatchAsync()
        {
            await Task.WhenAll(Enumerable.Range(0, 10).Select(async index =>
            {
                await using var db = new LolAnalyzerDbContext(options);
                await SyncAsync(db, index == 0 ? "owner" : $"p{index}", index == 0 ? 5 : 3, "LA1_TARGET");
            }));
            await using var read = new LolAnalyzerDbContext(options);
            var analysis = new PlayerAnalysisRepository(read);
            var pairs = new PlayerRelationshipRepository(read, relationshipAnalyzer);
            var detail = new MatchDetailQueryService(analysis, new MatchPremadeGroupService(pairs, analysis,
                pairDetector, groupDetector, new PremadeDetectionOptions()), new MatchFamiliarityService(analysis, metrics));
            Assert.Equal(9, (await detail.GetAsync("LA1_TARGET", "owner", TestContext.Current.CancellationToken))!.Familiarity!.KnownPlayers);
        }

        private async Task SyncAsync(LolAnalyzerDbContext db, string puuid, int count, string? excluded)
        {
            var ingestion = new MatchIngestionService(client, new MatchRepository(db), ingestionOptions, metrics);
            var synced = await ingestion.SynchronizeAsync(puuid, count, "LA1", TestContext.Current.CancellationToken, excluded);
            var analysis = new RepeatedPlayerAnalysisService(new PlayerAnalysisRepository(db));
            await analysis.RebuildAsync(puuid, TestContext.Current.CancellationToken);
            var relationships = new PlayerRelationshipAnalysisService(new PlayerRelationshipRepository(db, relationshipAnalyzer),
                relationshipAnalyzer, relationshipOptions);
            await relationships.UpdateIncrementallyAsync(synced.PersistedMatchIds, TestContext.Current.CancellationToken);
        }
    }

    private sealed record Measurement(string Architecture, string Scenario, string Temperature,
        double Milliseconds, double CpuMilliseconds, long AllocatedBytes, int Calls, long Hits);
}
