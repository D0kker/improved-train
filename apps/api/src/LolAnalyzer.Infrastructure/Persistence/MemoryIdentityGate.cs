namespace LolAnalyzer.Infrastructure.Persistence;

// InMemory does not enforce unique PUUID indexes. All player writers share
// this bounded process-wide gate; Riot HTTP downloads remain concurrent.
internal static class MemoryIdentityGate
{
    internal static SemaphoreSlim Instance { get; } = new(1, 1);
}
