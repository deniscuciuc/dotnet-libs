using CoreLibs.Startup;
using CoreLibs.Storage.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace CoreLibs.Storage.Startup;

public static class StorageStartupExtensions
{
    public static IServiceCollection AddCoreStorageStartup(this IServiceCollection services)
    {
        services.AddCoreStartup<StorageInitializationStartup>();
        services.AddCoreStartupGuard<IFileStorage>();
        return services;
    }
}
