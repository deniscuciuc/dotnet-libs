using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace CoreLibs.Localization.Cache;

public static class LocalizationCacheExtensions
{
    public static IServiceCollection AddCoreLocalizationRedisCache(
        this IServiceCollection services,
        Action<LocalizationCacheOptions>? configure = null)
    {
        if (configure is not null)
            services.Configure(configure);
        services.TryAddSingleton<ILocalizationDistributedCache, RedisLocalizationCache>();

        // Decorate existing ILocalizationCache with distributed write-through.
        // Finds the current registration, removes it, and wraps it with the decorator.
        var existing = services.LastOrDefault(d =>
            d.ServiceType == typeof(ILocalizationCache) && !d.IsKeyedService);
        if (existing is not null)
        {
            services.Remove(existing);
            services.AddSingleton<ILocalizationCache>(sp =>
            {
                var inner = existing.ImplementationType is not null
                    ? (ILocalizationCache)ActivatorUtilities.CreateInstance(sp, existing.ImplementationType)
                    : existing.ImplementationInstance is not null
                        ? (ILocalizationCache)existing.ImplementationInstance
                        : (ILocalizationCache)existing.ImplementationFactory!(sp);

                return new DistributedLocalizationCacheDecorator(
                    inner,
                    sp.GetRequiredService<ILocalizationDistributedCache>(),
                    sp.GetRequiredService<ILogger<DistributedLocalizationCacheDecorator>>());
            });
        }

        return services;
    }
}
