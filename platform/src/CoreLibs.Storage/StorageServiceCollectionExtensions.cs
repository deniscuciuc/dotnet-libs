using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CoreLibs.Storage;

public static class StorageServiceCollectionExtensions
{
    /// <summary>
    /// Registers the file storage provider determined by the <c>Storage</c> configuration section.
    /// </summary>
    public static IServiceCollection AddCoreFileStorage(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionPath = FileStorageOptions.DefaultSectionPath)
    {
        services
            .AddOptions<FileStorageOptions>()
            .Bind(configuration.GetSection(sectionPath))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var options = configuration
                          .GetSection(sectionPath)
                          .Get<FileStorageOptions>()
                      ?? new FileStorageOptions();

        RegisterProvider(services, configuration, options.Provider);

        return services;
    }

    private static void RegisterProvider(
        IServiceCollection services,
        IConfiguration configuration,
        StorageProviderKind provider)
    {
        var (typeName, assemblyName, methodName) = provider switch
        {
            StorageProviderKind.S3 => (
                "CoreLibs.Storage.S3.S3StorageServiceCollectionExtensions",
                "CoreLibs.Storage.S3",
                "AddS3Storage"),
            StorageProviderKind.Disk => (
                "CoreLibs.Storage.Disk.DiskStorageServiceCollectionExtensions",
                "CoreLibs.Storage.Disk",
                "AddDiskStorage"),
            _ => throw new InvalidOperationException($"Unsupported storage provider: {provider}")
        };

        var type = Type.GetType($"{typeName}, {assemblyName}")
                   ?? throw new InvalidOperationException(
                       $"Storage provider package '{assemblyName}' is not referenced. Add the package and retry.");

        var method = type.GetMethod(
                         methodName,
                         [typeof(IServiceCollection), typeof(IConfiguration)])
                     ?? throw new InvalidOperationException(
                         $"Provider registration method '{methodName}' was not found in '{typeName}'.");

        method.Invoke(null, [services, configuration]);
    }
}
