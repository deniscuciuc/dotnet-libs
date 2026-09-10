using CoreLibs.Cache;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CoreLibs.Redis;

public static class RedisClientExtensions
{
    /// <summary>
    /// Registers the Redis client from the <paramref name="configuration"/> section.
    /// </summary>
    public static IServiceCollection AddCoreRedis(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionPath = RedisOptions.DefaultSectionPath)
    {
        services
            .AddOptions<RedisOptions>()
            .Bind(configuration.GetSection(sectionPath))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return AddRedisCore(services);
    }

    /// <summary>
    /// Registers the Redis client using an explicit options delegate.
    /// </summary>
    public static IServiceCollection AddCoreRedis(
        this IServiceCollection services,
        Action<RedisOptions> configureOptions)
    {
        services
            .AddOptions<RedisOptions>()
            .Configure(configureOptions)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return AddRedisCore(services);
    }

    /// <summary>
    /// Registers the Redis cache layer (<see cref="ICache"/>) backed by Redis.
    /// </summary>
    public static IServiceCollection AddCoreRedisCache(this IServiceCollection services)
    {
        services.AddSingleton<ICacheSerializer, JsonCacheSerializer>();
        services.AddSingleton<RedisCache>();
        services.AddSingleton<ICache>(s => s.GetRequiredService<RedisCache>());

        return services;
    }

    private static IServiceCollection AddRedisCore(IServiceCollection services)
    {
        services.AddSingleton<RedisClient>();
        services.AddSingleton<IRedisClient>(s => s.GetRequiredService<RedisClient>());
        services.AddSingleton<IRedisConnection>(s => s.GetRequiredService<RedisClient>());

        return services;
    }
}
