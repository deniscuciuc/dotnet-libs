using DenisCuciuc.Platform.MongoDB.Seeding;
using DenisCuciuc.Platform.Startup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DenisCuciuc.Platform.MongoDB.Startup;

/// <summary>
/// Runs all registered MongoDB seeders sequentially.
/// </summary>
public sealed class MongoSeedingStartup : IStartup
{
    public int Order => 3;

    public async Task StartupAsync(IHostStartup host, CancellationToken cancellationToken = default)
    {
        var runner = host.Services.GetService<ISeederRunner>();
        if (runner is null)
        {
            host.Logger.LogDebug("No ISeederRunner registered â€” skipping seeding");
            return;
        }

        host.Logger.LogInformation("Running MongoDB seeders ...");

        await runner.RunAsync(cancellationToken);

        host.Logger.LogInformation("MongoDB seeding completed");
    }
}
