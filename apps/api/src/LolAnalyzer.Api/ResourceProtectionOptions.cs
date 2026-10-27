namespace LolAnalyzer.Api;

public sealed class ResourceProtectionOptions
{
    public const string SectionName = "ResourceProtection";
    public int ConcurrencyLimit { get; set; } = 8;
    public int QueueLimit { get; set; } = 16;
    public int QueueWaitMilliseconds { get; set; } = 5000;
    public int ClientBurst { get; set; } = 20;
    public int ClientRequestsPerMinute { get; set; } = 20;
    public int MaxTrackedClients { get; set; } = 1024;
    public int ClientIdleSeconds { get; set; } = 180;

    public void Validate()
    {
        if (ConcurrencyLimit is < 1 or > 64 || QueueLimit is < 0 or > 128
            || QueueWaitMilliseconds is < 10 or > 30000 || ClientBurst is < 1 or > 1000
            || ClientRequestsPerMinute is < 1 or > 10000 || MaxTrackedClients is < 1 or > 16384
            || ClientIdleSeconds is < 60 or > 86400
            || ClientIdleSeconds < ClientBurst * 60d / ClientRequestsPerMinute)
            throw new InvalidOperationException("Invalid resource protection limits; client retention must cover a full token refill.");
    }
}
