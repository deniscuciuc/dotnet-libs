using DenisCuciuc.Platform.MongoDB.Migrations;
using DenisCuciuc.Platform.Startup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DenisCuciuc.Platform.MongoDB.Startup;

/// <summary>
/// Runs pending MongoDB migrations sequentially up to the configured target version.
/// </summary>
public sealed class MongoMigrationStartup : IStartup
{
    public int Order => 2;

    public async Task StartupAsync(IHostStartup host, CancellationToken cancellationToken = default)
    {
        var runner = host.Services.GetService<IMigrationRunner>();
        if (runner is null)
        {
            host.Logger.LogDebug("No IMigrationRunner registered â€” skipping migrations");
            return;
        }

        var version = host.Services.GetService<MigrationTargetVersion>()?.Version
                      ?? MigrationVersion.Latest;

        host.Logger.LogInformation("Running MongoDB migrations (target: {Version}) ...", version);

        await runner.RunAsync(version, cancellationToken);

        host.Logger.LogInformation("MongoDB migrations completed");
    }
}

/// <summary>
/// Holds the target <see cref="MigrationVersion"/> resolved from DI.
/// Register via <see cref="MongoDBStartupExtensions.AddPlatformMongoDBStartup"/>.
/// </summary>
public sealed class MigrationTargetVersion(MigrationVersion version)
{
    public MigrationVersion Version { get; } = version;
}
