using LolAnalyzer.Domain.Entities;

namespace LolAnalyzer.Application.Jobs;

public sealed class PlayerRefreshScheduleService(
    IPlayerRefreshScheduleRepository scheduleRepository,
    IAnalysisJobRepository jobRepository,
    TimeProvider timeProvider)
{
    public const int MinimumIntervalMinutes = 15;
    public const int MaximumIntervalMinutes = 10_080;
    public const int MaximumRequestedCount = 200;

    public async Task<PlayerRefreshScheduleView?> ConfigureAsync(
        string puuid,
        int requestedCount,
        int intervalMinutes,
        bool enabled,
        CancellationToken cancellationToken)
    {
        Validate(puuid, requestedCount, intervalMinutes);
        var schedule = await scheduleRepository.UpsertAsync(
            puuid,
            requestedCount,
            intervalMinutes,
            enabled,
            timeProvider.GetUtcNow(),
            cancellationToken).ConfigureAwait(false);
        return schedule is null ? null : Map(schedule);
    }

    public async Task<PlayerRefreshScheduleView?> FindAsync(string puuid, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(puuid);
        var schedule = await scheduleRepository.FindAsync(puuid, cancellationToken).ConfigureAwait(false);
        return schedule is null ? null : Map(schedule);
    }

    public async Task<bool> EnqueueNextDueAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var schedule = await scheduleRepository.ClaimNextDueAsync(now, cancellationToken).ConfigureAwait(false);
        if (schedule is null)
        {
            return false;
        }

        await jobRepository.AddOrGetActiveAsync(new AnalysisJob
        {
            Puuid = schedule.Puuid,
            RequestedCount = schedule.RequestedCount,
            Status = AnalysisJobStatus.Queued,
            CreatedAt = now,
            UpdatedAt = now,
        }, cancellationToken).ConfigureAwait(false);
        return true;
    }

    private static void Validate(string puuid, int requestedCount, int intervalMinutes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(puuid);
        ArgumentOutOfRangeException.ThrowIfLessThan(requestedCount, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(requestedCount, MaximumRequestedCount);
        ArgumentOutOfRangeException.ThrowIfLessThan(intervalMinutes, MinimumIntervalMinutes);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(intervalMinutes, MaximumIntervalMinutes);
    }

    private static PlayerRefreshScheduleView Map(PlayerRefreshSchedule schedule) => new(
        schedule.Puuid,
        schedule.RequestedCount,
        schedule.IntervalMinutes,
        schedule.Enabled,
        schedule.NextRunAt,
        schedule.LastEnqueuedAt,
        schedule.UpdatedAt);
}

public sealed record PlayerRefreshScheduleView(
    string Puuid,
    int MatchCount,
    int IntervalMinutes,
    bool Enabled,
    DateTimeOffset NextRunAt,
    DateTimeOffset? LastEnqueuedAt,
    DateTimeOffset UpdatedAt);
