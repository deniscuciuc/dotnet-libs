using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CoreLibs.LiveConfig.Redis;

public static class RedisLiveConfigExtensions
{
    /// <summary>
    /// Registers the Redis config distributor.
    /// Assumes <see cref="CoreLibs.Redis.IRedisClient"/> is already registered
    /// (e.g. via <c>AddRedis()</c> from CoreLibs.Redis).
    /// </summary>
    public static IServiceCollection AddLiveConfigRedis(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionPath = RedisLiveConfigOptions.SectionPath)
    {
        services.Configure<RedisLiveConfigOptions>(configuration.GetSection(sectionPath));
        services.AddSingleton<IConfigDistributor, RedisConfigDistributor>();
        return services;
    }

    /// <summary>
    /// Registers the Redis config distributor with inline options.
    /// </summary>
    public static IServiceCollection AddLiveConfigRedis(
        this IServiceCollection services,
        Action<RedisLiveConfigOptions>? configure = null)
    {
        if (configure is not null)
            services.Configure(configure);
        else
            services.Configure<RedisLiveConfigOptions>(_ => { });

        services.AddSingleton<IConfigDistributor, RedisConfigDistributor>();
        return services;
    }
}
