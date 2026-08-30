using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LolAnalyzer.Infrastructure.Health;

public sealed class RedisHealthCheck(string host, int port) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(host, port, cancellationToken).ConfigureAwait(false);
            await using var stream = client.GetStream();
            var ping = "*1\r\n$4\r\nPING\r\n"u8.ToArray();
            await stream.WriteAsync(ping, cancellationToken).ConfigureAwait(false);

            var response = new byte[7];
            var bytesRead = await stream.ReadAsync(response, cancellationToken).ConfigureAwait(false);
            return Encoding.ASCII.GetString(response, 0, bytesRead).StartsWith("+PONG", StringComparison.Ordinal)
                ? HealthCheckResult.Healthy("Redis responded to PING.")
                : HealthCheckResult.Unhealthy("Redis returned an unexpected response.");
        }
        catch (Exception exception) when (exception is SocketException or IOException or OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy("Redis TCP endpoint is unavailable.", exception);
        }
    }
}
