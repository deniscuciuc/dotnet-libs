using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CoreLibs.Localization.Runtime;

public static class LocalizationServiceCollectionExtensions
{
    /// <summary>
    /// Registers core localization services: ILocalizationCache + ILocalizer.
    /// </summary>
    public static IServiceCollection AddCoreLocalization(
        this IServiceCollection services,
        Action<LocalizationOptions>? configure = null)
    {
        services.TryAddSingleton<ILocalizationCache, LocalizationCache>();
        services.TryAddSingleton<ILocalizer, Localizer>();
        if (configure is not null)
            services.Configure(configure);
        return services;
    }
}
