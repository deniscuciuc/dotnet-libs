using CoreLibs.Startup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CoreLibs.Redis.Startup;

/// <summary>
/// Connects all registered <see cref="IRedisConnection"/> instances in parallel
/// and marks the <see cref="StartupGuard{T}"/> for <see cref="IRedisClient"/> as configured.
/// </summary>
public sealed class RedisConnectStartup : IStartup
{
    public int Order => 0;

    public async Task StartupAsync(IHostStartup host, CancellationToken cancellationToken = default)
    {
        var connections = host.Services.GetServices<IRedisConnection>().ToList();

        host.Logger.LogInformation("Connecting to Redis ({Count} connection(s)) ...", connections.Count);

        await Task.WhenAll(connections.Select(x => x.ConnectAsync()));

        host.MarkConfigured<IRedisClient>();

        host.Logger.LogInformation("Redis connected");
    }
}
