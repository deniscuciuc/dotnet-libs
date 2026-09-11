using CoreLibs.Storage.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CoreLibs.Storage.Disk;

public static class DiskStorageServiceCollectionExtensions
{
    public static IServiceCollection AddDiskStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<DiskStorageOptions>()
            .Bind(configuration.GetSection(DiskStorageOptions.DefaultSectionPath))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.TryAddSingleton<IFileStorage, DiskFileStorage>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IStorageInitializer, DiskStorageInitializer>());

        return services;
    }
}
