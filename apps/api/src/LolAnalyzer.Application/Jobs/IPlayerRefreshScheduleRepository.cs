using LolAnalyzer.Domain.Entities;

namespace LolAnalyzer.Application.Jobs;

public interface IPlayerRefreshScheduleRepository
{
    Task<PlayerRefreshSchedule?> FindAsync(string puuid, CancellationToken cancellationToken);

    Task<PlayerRefreshSchedule?> UpsertAsync(
        string puuid,
        int requestedCount,
        int intervalMinutes,
        bool enabled,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<PlayerRefreshSchedule?> ClaimNextDueAsync(DateTimeOffset now, CancellationToken cancellationToken);
}
