using LolAnalyzer.Application.Jobs;
using LolAnalyzer.Domain.Entities;
using Xunit;

namespace LolAnalyzer.UnitTests;

public sealed class PlayerRefreshScheduleServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 31, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ConfigureIsExplicitAndUsesInjectedClockForNextRun()
    {
        var schedules = new InMemoryScheduleRepository();
        var service = new PlayerRefreshScheduleService(
            schedules,
            new RecordingJobRepository(),
            new FixedTimeProvider(Now));

        var schedule = await service.ConfigureAsync(
            "test-puuid",
            100,
            60,
            true,
            TestContext.Current.CancellationToken);

        Assert.NotNull(schedule);
        Assert.True(schedule.Enabled);
        Assert.Equal(100, schedule.MatchCount);
        Assert.Equal(Now.AddHours(1), schedule.NextRunAt);
    }

    [Fact]
    public async Task DueScheduleEnqueuesOnceAndAdvancesBeforeAnotherClaim()
    {
        var schedules = new InMemoryScheduleRepository
        {
            Due = new PlayerRefreshSchedule
            {
                Puuid = "test-puuid",
                RequestedCount = 50,
                IntervalMinutes = 30,
                Enabled = true,
                NextRunAt = Now,
            },
        };
        var jobs = new RecordingJobRepository();
        var service = new PlayerRefreshScheduleService(schedules, jobs, new FixedTimeProvider(Now));

        Assert.True(await service.EnqueueNextDueAsync(TestContext.Current.CancellationToken));
        Assert.False(await service.EnqueueNextDueAsync(TestContext.Current.CancellationToken));

        var job = Assert.Single(jobs.Added);
        Assert.Equal("test-puuid", job.Puuid);
        Assert.Equal(50, job.RequestedCount);
        Assert.Equal(Now, schedules.LastClaimedAt);
    }

    [Fact]
    public async Task DisabledScheduleDoesNotEnqueue()
    {
        var schedules = new InMemoryScheduleRepository
        {
            Due = new PlayerRefreshSchedule
            {
                Puuid = "test-puuid",
                RequestedCount = 20,
                IntervalMinutes = 60,
                Enabled = false,
                NextRunAt = Now,
            },
        };
        var jobs = new RecordingJobRepository();
        var service = new PlayerRefreshScheduleService(schedules, jobs, new FixedTimeProvider(Now));

        Assert.False(await service.EnqueueNextDueAsync(TestContext.Current.CancellationToken));
        Assert.Empty(jobs.Added);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class InMemoryScheduleRepository : IPlayerRefreshScheduleRepository
    {
        public PlayerRefreshSchedule? Due { get; set; }

        public DateTimeOffset? LastClaimedAt { get; private set; }

        public Task<PlayerRefreshSchedule?> FindAsync(string puuid, CancellationToken cancellationToken) =>
            Task.FromResult(Due?.Puuid == puuid ? Due : null);

        public Task<PlayerRefreshSchedule?> UpsertAsync(
            string puuid,
            int requestedCount,
            int intervalMinutes,
            bool enabled,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            Due = new PlayerRefreshSchedule
            {
                Puuid = puuid,
                RequestedCount = requestedCount,
                IntervalMinutes = intervalMinutes,
                Enabled = enabled,
                NextRunAt = now.AddMinutes(intervalMinutes),
                CreatedAt = now,
                UpdatedAt = now,
            };
            return Task.FromResult<PlayerRefreshSchedule?>(Due);
        }

        public Task<PlayerRefreshSchedule?> ClaimNextDueAsync(
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            if (Due is null || !Due.Enabled || Due.NextRunAt > now)
            {
                return Task.FromResult<PlayerRefreshSchedule?>(null);
            }

            var claimed = Due;
            LastClaimedAt = now;
            Due.NextRunAt = now.AddMinutes(Due.IntervalMinutes);
            return Task.FromResult<PlayerRefreshSchedule?>(claimed);
        }
    }

    private sealed class RecordingJobRepository : IAnalysisJobRepository
    {
        public List<AnalysisJob> Added { get; } = [];

        public Task<AnalysisJob> AddOrGetActiveAsync(AnalysisJob job, CancellationToken cancellationToken)
        {
            Added.Add(job);
            return Task.FromResult(job);
        }

        public Task<AnalysisJob?> FindAsync(Guid jobId, CancellationToken cancellationToken) =>
            Task.FromResult<AnalysisJob?>(null);

        public Task<AnalysisJob?> ClaimNextAsync(
            DateTimeOffset now,
            DateTimeOffset staleBefore,
            CancellationToken cancellationToken) =>
            Task.FromResult<AnalysisJob?>(null);

        public Task UpdateProgressAsync(
            Guid jobId,
            int matchesProcessed,
            DateTimeOffset now,
            CancellationToken cancellationToken) => Task.CompletedTask;

        public Task CompleteAsync(
            Guid jobId,
            int matchesProcessed,
            DateTimeOffset now,
            CancellationToken cancellationToken) => Task.CompletedTask;

        public Task FailAsync(
            Guid jobId,
            string errorCode,
            DateTimeOffset now,
            CancellationToken cancellationToken) => Task.CompletedTask;

        public Task RequeueAsync(Guid jobId, DateTimeOffset now, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<AnalysisJob?> CancelAsync(
            Guid jobId,
            DateTimeOffset now,
            CancellationToken cancellationToken) => Task.FromResult<AnalysisJob?>(null);

        public Task<bool> IsCancelledAsync(Guid jobId, CancellationToken cancellationToken) =>
            Task.FromResult(false);
    }
}
