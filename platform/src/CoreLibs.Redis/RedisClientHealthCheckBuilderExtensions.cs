using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace CoreLibs.Redis;

public static class RedisClientHealthCheckBuilderExtensions
{
    public static IHealthChecksBuilder AddRedisHealthCheck(this IHealthChecksBuilder builder)
    {
        return builder.AddRedis(s => s.GetRequiredService<IConnectionMultiplexer>());
    }
}
