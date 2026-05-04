using DenisCuciuc.Platform.Startup;
using DenisCuciuc.Platform.Storage.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DenisCuciuc.Platform.Storage.Startup;

public sealed class StorageInitializationStartup : IStartup
{
    public int Order => 0;

    public async Task StartupAsync(IHostStartup host, CancellationToken cancellationToken = default)
    {
        var storage = host.Services.GetRequiredService<IFileStorage>();
        _ = storage;

        var initializers = host.Services.GetServices<IStorageInitializer>().ToArray();
        if (initializers.Length == 0)
        {
            host.Logger.LogDebug("No storage initializers registered");
        }
        else
        {
            host.Logger.LogInformation("Running {Count} storage initializer(s) ...", initializers.Length);

            foreach (var initializer in initializers)
            {
                await initializer.InitializeAsync(cancellationToken);
            }
        }

        host.MarkConfigured<IFileStorage>();
    }
}
