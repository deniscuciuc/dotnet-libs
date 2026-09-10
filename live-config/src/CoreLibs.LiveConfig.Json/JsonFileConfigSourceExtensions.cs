using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CoreLibs.LiveConfig.Json;

public static class JsonFileConfigSourceExtensions
{
    public static IServiceCollection AddLiveConfigJsonSource(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionPath = JsonFileConfigSourceOptions.SectionPath)
    {
        var options = configuration.GetSection(sectionPath).Get<JsonFileConfigSourceOptions>()
                      ?? new JsonFileConfigSourceOptions();
        services.AddSingleton(options);
        services.AddSingleton<IConfigSource, JsonFileConfigSource>();
        return services;
    }

    public static IServiceCollection AddLiveConfigJsonSource(
        this IServiceCollection services,
        Action<JsonFileConfigSourceOptions>? configure = null)
    {
        var options = new JsonFileConfigSourceOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);
        services.AddSingleton<IConfigSource, JsonFileConfigSource>();
        return services;
    }
}
