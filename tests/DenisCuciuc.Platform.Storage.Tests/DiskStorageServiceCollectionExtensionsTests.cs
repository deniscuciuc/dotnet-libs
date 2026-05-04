using DenisCuciuc.Platform.Storage.Abstractions;
using DenisCuciuc.Platform.Storage.Disk;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DenisCuciuc.Platform.Storage.Tests;

public class DiskStorageServiceCollectionExtensionsTests
{
    [Fact]
    public void AddDiskStorage_RegistersIFileStorage()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:Disk:BasePath"] = Path.GetTempPath()
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDiskStorage(config);

        var provider = services.BuildServiceProvider();
        var storage = provider.GetRequiredService<IFileStorage>();

        Assert.NotNull(storage);
        Assert.IsType<DiskFileStorage>(storage);
    }

    [Fact]
    public void AddDiskStorage_RegistersIStorageInitializer()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:Disk:BasePath"] = Path.GetTempPath()
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDiskStorage(config);

        var provider = services.BuildServiceProvider();
        var initializer = provider.GetRequiredService<IStorageInitializer>();

        Assert.NotNull(initializer);
        Assert.IsType<DiskStorageInitializer>(initializer);
    }

    [Fact]
    public void AddDiskStorage_TryAddSingleton_DoesNotDuplicateRegistration()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:Disk:BasePath"] = Path.GetTempPath()
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDiskStorage(config);
        services.AddDiskStorage(config);

        var count = services.Count(s => s.ServiceType == typeof(IFileStorage));
        Assert.Equal(1, count);
    }
}
