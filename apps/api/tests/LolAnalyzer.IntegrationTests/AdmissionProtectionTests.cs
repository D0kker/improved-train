using System.Net;
using LolAnalyzer.Api;
using LolAnalyzer.Application.Observability;
using LolAnalyzer.Testing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LolAnalyzer.IntegrationTests;

public sealed class AdmissionProtectionTests
{
    [Fact]
    public async Task MaximumConcurrencyAndBoundedFifoQueueRejectOverload()
    {
        using var metrics = new OperationalMetrics();
        using var gate = new ApiResourceGate(Settings(), metrics, TimeProvider.System);
        using var services = Services();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = gate.InvokeAsync(Context(services), _ => release.Task);
        var secondRan = false;
        var second = gate.InvokeAsync(Context(services), _ => { secondRan = true; return Task.CompletedTask; });
        var rejected = Context(services);
        await gate.InvokeAsync(rejected, _ => throw new InvalidOperationException("Must not run"));
        Assert.Equal(503, rejected.Response.StatusCode);
        Assert.Equal("1", rejected.Response.Headers.RetryAfter);
        Assert.False(secondRan);
        Assert.Equal(1, metrics.Snapshot().Resources!.ActiveRequests);
        Assert.Equal(1, metrics.Snapshot().Resources!.QueuedRequests);
        release.SetResult();
        await Task.WhenAll(first, second);
        Assert.True(secondRan);
        var result = metrics.Snapshot().Resources!;
        Assert.Equal(1, result.PeakActiveRequests);
        Assert.Equal(0, result.ActiveRequests);
        Assert.Equal(0, result.QueuedRequests);
        Assert.Equal(1, result.RejectedRequests);
    }

    [Fact]
    public async Task QueueWaitHasIndependentDeadlineAndCancelledWaiterFreesSlot()
    {
        using var metrics = new OperationalMetrics();
        using var gate = new ApiResourceGate(Settings(queueWait: 50), metrics, TimeProvider.System);
        using var services = Services();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = gate.InvokeAsync(Context(services), _ => release.Task);
        var expired = Context(services);
        await gate.InvokeAsync(expired, _ => throw new InvalidOperationException("Must not run"));
        Assert.Equal(503, expired.Response.StatusCode);
        using var cancellation = new CancellationTokenSource();
        var abandoned = Context(services);
        abandoned.RequestAborted = cancellation.Token;
        var waiting = gate.InvokeAsync(abandoned, _ => throw new InvalidOperationException("Must not run"));
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiting);
        Assert.Equal(0, metrics.Snapshot().Resources!.QueuedRequests);
        release.SetResult();
        await first;
        var fresh = Context(services);
        await gate.InvokeAsync(fresh, _ => Task.CompletedTask);
        Assert.Equal(200, fresh.Response.StatusCode);
    }

    [Fact]
    public async Task ClientQuotaIgnoresSpoofedForwardedHeadersAndRefills()
    {
        var clock = new AdjustableTimeProvider();
        using var metrics = new OperationalMetrics();
        using var gate = new ApiResourceGate(Settings(burst: 1, requests: 1), metrics, clock);
        using var services = Services();
        await gate.InvokeAsync(Context(services), _ => Task.CompletedTask);
        var spoof = Context(services);
        spoof.Request.Headers["X-Forwarded-For"] = "203.0.113.222";
        spoof.Request.Headers["Forwarded"] = "for=203.0.113.221";
        await gate.InvokeAsync(spoof, _ => throw new InvalidOperationException("Quota bypass"));
        Assert.Equal(429, spoof.Response.StatusCode);
        Assert.Equal("60", spoof.Response.Headers.RetryAfter);
        var other = Context(services, "192.0.2.2");
        await gate.InvokeAsync(other, _ => Task.CompletedTask);
        Assert.Equal(200, other.Response.StatusCode);
        clock.Advance(TimeSpan.FromMinutes(1));
        var refilled = Context(services);
        await gate.InvokeAsync(refilled, _ => Task.CompletedTask);
        Assert.Equal(200, refilled.Response.StatusCode);
    }

    [Fact]
    public async Task ClientBookkeepingIsBoundedAndExpiredClientsCanBeReleased()
    {
        var clock = new AdjustableTimeProvider();
        using var metrics = new OperationalMetrics();
        using var gate = new ApiResourceGate(Settings(maxClients: 1), metrics, clock);
        using var services = Services();
        await gate.InvokeAsync(Context(services), _ => Task.CompletedTask);
        var overflow = Context(services, "192.0.2.2");
        await gate.InvokeAsync(overflow, _ => Task.CompletedTask);
        Assert.Equal(503, overflow.Response.StatusCode);
        Assert.Equal(1, gate.TrackedClients);
        clock.Advance(TimeSpan.FromMinutes(4));
        var refreshed = Context(services, "192.0.2.2");
        await gate.InvokeAsync(refreshed, _ => Task.CompletedTask);
        Assert.Equal(200, refreshed.Response.StatusCode);
        Assert.Equal(1, gate.TrackedClients);
    }

    [Fact]
    public async Task HealthBypassesSaturatedGateAndInvalidInputsNeverReachRiot()
    {
        await using var factory = new StatelessApiFactory();
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/v1/players/by-riot-id/" + new string('a', 65) + "/TEST/analysis", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, factory.Riot.AccountCalls);
        using var metrics = new OperationalMetrics();
        using var gate = new ApiResourceGate(Settings(), metrics, TimeProvider.System);
        using var services = Services();
        var health = Context(services);
        health.Request.Path = "/health/ready";
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var active = gate.InvokeAsync(Context(services), _ => release.Task);
        var queued = gate.InvokeAsync(Context(services), _ => Task.CompletedTask);
        await gate.InvokeAsync(health, _ => Task.CompletedTask);
        Assert.Equal(200, health.Response.StatusCode);
        Assert.Equal(1, metrics.Snapshot().Resources!.ActiveRequests);
        Assert.Equal(1, metrics.Snapshot().Resources!.QueuedRequests);
        release.SetResult();
        await Task.WhenAll(active, queued);
    }

    private static ResourceProtectionOptions Settings(int queueWait = 5000, int burst = 100, int requests = 100, int maxClients = 1024) => new()
    { ConcurrencyLimit = 1, QueueLimit = 1, QueueWaitMilliseconds = queueWait, ClientBurst = burst, ClientRequestsPerMinute = requests, MaxTrackedClients = maxClients };

    private static ServiceProvider Services() => new ServiceCollection().AddLogging().AddOptions().BuildServiceProvider();
    private static DefaultHttpContext Context(IServiceProvider services, string address = "192.0.2.1")
    {
        var context = new DefaultHttpContext { RequestServices = services };
        context.Connection.RemoteIpAddress = IPAddress.Parse(address);
        context.Request.Path = "/api/v1/matches/LA1_TEST/analyze";
        context.Response.Body = new MemoryStream();
        return context;
    }
}
