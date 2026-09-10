using CoreLibs.LiveConfig;
using Microsoft.Extensions.DependencyInjection;

namespace CoreLibs.Localization.LiveConfig;

public static class LocalizationLiveConfigExtensions
{
    /// <summary>
    /// Registers <see cref="LocalizationConfigApplier"/> for each culture.
    /// LiveConfig will call ApplyAsync when a localization config is updated.
    /// </summary>
    /// <param name="services">DI container.</param>
    /// <param name="cultures">
    /// The list of cultures to register appliers for, e.g. ["ru", "en"].
    /// Config types will be "localization:ru", "localization:en".
    /// </param>
    public static IServiceCollection AddCoreLocalizationLiveConfig(
        this IServiceCollection services,
        IEnumerable<string> cultures)
    {
        ArgumentNullException.ThrowIfNull(cultures);

        foreach (var culture in cultures)
            services.AddSingleton<IConfigApplier>(sp =>
                new LocalizationConfigApplier(
                    sp.GetRequiredService<ILocalizationCache>(),
                    sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<LocalizationConfigApplier>>())
                {
                    ConfigType = LocalizationConstants.ConfigType(culture)
                });
        return services;
    }
}
