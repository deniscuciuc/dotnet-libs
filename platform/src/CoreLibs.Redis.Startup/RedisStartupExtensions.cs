using CoreLibs.Startup;
using Microsoft.Extensions.DependencyInjection;

namespace CoreLibs.Redis.Startup;

public static class RedisStartupExtensions
{
    /// <summary>
    /// Registers the full Redis startup pipeline:
    /// <see cref="RedisConnectStartup"/> (order 0)
    /// and a <see cref="StartupGuard{T}"/> for <see cref="IRedisClient"/>.
    /// </summary>
    public static IServiceCollection AddCoreRedisStartup(this IServiceCollection services)
    {
        services.AddCoreStartup<RedisConnectStartup>();
        services.AddCoreStartupGuard<IRedisClient>();
        return services;
    }

    /// <summary>
    /// Marks the <see cref="StartupGuard{T}"/> for <see cref="IRedisClient"/> as configured.
    /// Called automatically by <see cref="RedisConnectStartup"/>; only use this if you are
    /// doing fully manual orchestration.
    /// </summary>
    public static void MarkRedisConfigured(this IHostStartup hostStartup) =>
        hostStartup.MarkConfigured<IRedisClient>();
}
