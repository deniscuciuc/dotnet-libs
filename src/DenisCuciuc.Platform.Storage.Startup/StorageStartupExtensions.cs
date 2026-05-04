using DenisCuciuc.Platform.Startup;
using DenisCuciuc.Platform.Storage.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace DenisCuciuc.Platform.Storage.Startup;

public static class StorageStartupExtensions
{
    public static IServiceCollection AddPlatformStorageStartup(this IServiceCollection services)
    {
        services.AddPlatformStartup<StorageInitializationStartup>();
        services.AddPlatformStartupGuard<IFileStorage>();
        return services;
    }
}
