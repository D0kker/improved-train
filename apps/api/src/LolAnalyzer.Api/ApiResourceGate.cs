using System.Diagnostics;
using System.Globalization;
using System.Threading.RateLimiting;
using LolAnalyzer.Application.Observability;

namespace LolAnalyzer.Api;

/// <summary>One bounded FIFO admission gate per process; client state contains only quota bookkeeping.</summary>
public sealed class ApiResourceGate : IDisposable
{
    private readonly ConcurrencyLimiter limiter;
    private readonly ResourceProtectionOptions options;
    private readonly OperationalMetrics metrics;
    private readonly TimeProvider clock;
    private readonly object clientGate = new();
    private readonly Dictionary<string, ClientBucket> clients = new(StringComparer.Ordinal);

    public ApiResourceGate(ResourceProtectionOptions options, OperationalMetrics metrics, TimeProvider clock)
    {
        options.Validate();
        this.options = options;
        this.metrics = metrics;
        this.clock = clock;
        limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
        { PermitLimit = options.ConcurrencyLimit, QueueLimit = options.QueueLimit, QueueProcessingOrder = QueueProcessingOrder.OldestFirst });
    }

    public int TrackedClients { get { lock (clientGate) return clients.Count; } }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (!context.Request.Path.StartsWithSegments("/api/v1") && context.Request.Path != "/metrics")
        { await next(context).ConfigureAwait(false); return; }
        // Never trust X-Forwarded-For/Forwarded supplied by a client or unconfigured proxy.
        var client = context.Connection.RemoteIpAddress?.MapToIPv6().ToString() ?? "unknown";
        var quota = CheckClient(client);
        if (quota.Status != 0)
        {
            await RejectAsync(context, quota.Status, quota.Retry, quota.Status == 429 ? "client_quota" : "client_capacity").ConfigureAwait(false);
            return;
        }
        var started = Stopwatch.GetTimestamp();
        RateLimitLease lease;
        using (var waiting = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted))
        {
            waiting.CancelAfter(options.QueueWaitMilliseconds);
            var pending = limiter.AcquireAsync(1, waiting.Token);
            var queued = !pending.IsCompleted;
            if (queued) metrics.QueueEntered();
            try { lease = await pending.ConfigureAwait(false); }
            catch (OperationCanceledException) when (!context.RequestAborted.IsCancellationRequested)
            {
                await RejectAsync(context, 503, 1, "queue_timeout").ConfigureAwait(false);
                return;
            }
            finally { metrics.QueueExited(Stopwatch.GetElapsedTime(started), queued); }
        }
        using (lease)
        {
            if (!lease.IsAcquired) { await RejectAsync(context, 503, 1, "capacity").ConfigureAwait(false); return; }
            context.RequestAborted.ThrowIfCancellationRequested();
            metrics.WorkStarted();
            try { await next(context).ConfigureAwait(false); }
            finally { metrics.WorkFinished(); }
        }
    }

    private (int Status, int Retry) CheckClient(string client)
    {
        var now = clock.GetUtcNow();
        lock (clientGate)
        {
            if (!clients.TryGetValue(client, out var bucket))
            {
                if (clients.Count >= options.MaxTrackedClients)
                    foreach (var expired in clients.Where(pair => now - pair.Value.LastSeen >= TimeSpan.FromSeconds(options.ClientIdleSeconds))
                        .Select(pair => pair.Key).ToArray()) clients.Remove(expired);
                if (clients.Count >= options.MaxTrackedClients) return (503, options.ClientIdleSeconds);
                bucket = new ClientBucket(options.ClientBurst, now);
                clients.Add(client, bucket);
            }
            bucket.Tokens = Math.Min(options.ClientBurst, bucket.Tokens + Math.Max(0, (now - bucket.LastSeen).TotalMinutes) * options.ClientRequestsPerMinute);
            bucket.LastSeen = now;
            if (bucket.Tokens < 1) return (429, Math.Max(1, (int)Math.Ceiling((1 - bucket.Tokens) * 60 / options.ClientRequestsPerMinute)));
            bucket.Tokens--;
            return (0, 0);
        }
    }

    private async Task RejectAsync(HttpContext context, int status, int retry, string reason)
    {
        metrics.RecordRejection(reason);
        context.Response.Headers.RetryAfter = retry.ToString(CultureInfo.InvariantCulture);
        await Results.Problem(statusCode: status, title: status == 429 ? "Client request limit reached. Please wait before retrying."
            : "Analysis capacity is temporarily unavailable. Please retry later.").ExecuteAsync(context).ConfigureAwait(false);
    }

    public void Dispose() => limiter.Dispose();
    private sealed class ClientBucket(double tokens, DateTimeOffset now)
    {
        public double Tokens { get; set; } = tokens;
        public DateTimeOffset LastSeen { get; set; } = now;
    }
}
