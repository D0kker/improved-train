using System.Data;
using LolAnalyzer.Application.Jobs;
using LolAnalyzer.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LolAnalyzer.Infrastructure.Persistence;

public sealed class PlayerRefreshScheduleRepository(LolAnalyzerDbContext dbContext)
    : IPlayerRefreshScheduleRepository
{
    public Task<PlayerRefreshSchedule?> FindAsync(string puuid, CancellationToken cancellationToken) =>
        dbContext.PlayerRefreshSchedules
            .AsNoTracking()
            .SingleOrDefaultAsync(schedule => schedule.Puuid == puuid, cancellationToken);

    public async Task<PlayerRefreshSchedule?> UpsertAsync(
        string puuid,
        int requestedCount,
        int intervalMinutes,
        bool enabled,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var playerExists = await dbContext.Players
            .AsNoTracking()
            .AnyAsync(player => player.Puuid == puuid, cancellationToken)
            .ConfigureAwait(false);
        if (!playerExists)
        {
            return null;
        }

        var schedule = await dbContext.PlayerRefreshSchedules
            .SingleOrDefaultAsync(candidate => candidate.Puuid == puuid, cancellationToken)
            .ConfigureAwait(false);
        if (schedule is null)
        {
            schedule = new PlayerRefreshSchedule
            {
                Puuid = puuid,
                CreatedAt = now,
            };
            dbContext.PlayerRefreshSchedules.Add(schedule);
        }

        schedule.RequestedCount = requestedCount;
        schedule.IntervalMinutes = intervalMinutes;
        schedule.Enabled = enabled;
        schedule.NextRunAt = now.AddMinutes(intervalMinutes);
        schedule.UpdatedAt = now;
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return schedule;
    }

    public async Task<PlayerRefreshSchedule?> ClaimNextDueAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database
            .BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken)
            .ConfigureAwait(false);
        var schedule = await dbContext.PlayerRefreshSchedules
            .FromSqlInterpolated($"""
                SELECT *
                FROM player_refresh_schedules
                WHERE enabled AND next_run_at <= {now}
                ORDER BY next_run_at, puuid
                FOR UPDATE SKIP LOCKED
                LIMIT 1
                """)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        if (schedule is null)
        {
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return null;
        }

        schedule.LastEnqueuedAt = now;
        schedule.NextRunAt = now.AddMinutes(schedule.IntervalMinutes);
        schedule.UpdatedAt = now;
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return schedule;
    }
}
