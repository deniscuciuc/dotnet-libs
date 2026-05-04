using Microsoft.Extensions.Configuration;

namespace DenisCuciuc.Platform.Redis;

public static class RedisOptionsExtensions
{
    public static RedisOptions GetRedisOptions(
        this IConfiguration configuration,
        string sectionPath = RedisOptions.DefaultSectionPath)
    {
        return configuration.GetSection(sectionPath).Get<RedisOptions>()
               ?? throw new InvalidOperationException(
                   $"Redis configuration section '{sectionPath}' is missing or empty.");
    }
}
