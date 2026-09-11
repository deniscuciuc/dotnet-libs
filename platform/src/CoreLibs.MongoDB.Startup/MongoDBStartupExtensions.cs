using CoreLibs.MongoDB.Migrations;
using CoreLibs.Startup;
using Microsoft.Extensions.DependencyInjection;

namespace CoreLibs.MongoDB.Startup;

public static class MongoDBStartupExtensions
{
    /// <summary>
    /// Registers the full MongoDB startup pipeline:
    /// <see cref="MongoConnectStartup"/> (order 0),
    /// <see cref="MongoIndexStartup"/> (order 1),
    /// <see cref="MongoMigrationStartup"/> (order 2),
    /// <see cref="MongoSeedingStartup"/> (order 3),
    /// and a <see cref="StartupGuard{T}"/> for <see cref="IMongoDBProvider"/>.
    /// </summary>
    public static IServiceCollection AddCoreMongoDBStartup(
        this IServiceCollection services,
        MigrationVersion? targetVersion = null)
    {
        services.AddCoreStartup<MongoConnectStartup>();
        services.AddCoreStartup<MongoIndexStartup>();
        services.AddCoreStartup<MongoMigrationStartup>();
        services.AddCoreStartup<MongoSeedingStartup>();
        services.AddCoreStartupGuard<IMongoDBProvider>();

        services.AddSingleton(new MigrationTargetVersion(targetVersion ?? MigrationVersion.Latest));

        return services;
    }

    /// <summary>
    /// Marks the <see cref="StartupGuard{T}"/> for <see cref="IMongoDBProvider"/> as configured.
    /// Called automatically by <see cref="MongoConnectStartup"/>; only use this if you are
    /// doing fully manual orchestration.
    /// </summary>
    public static void MarkMongoDBConfigured(this IHostStartup hostStartup) =>
        hostStartup.MarkConfigured<IMongoDBProvider>();
}
