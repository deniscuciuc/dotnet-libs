using DenisCuciuc.Platform.MongoDB.Migrations;
using DenisCuciuc.Platform.Startup;
using Microsoft.Extensions.DependencyInjection;

namespace DenisCuciuc.Platform.MongoDB.Startup;

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
    public static IServiceCollection AddPlatformMongoDBStartup(
        this IServiceCollection services,
        MigrationVersion? targetVersion = null)
    {
        services.AddPlatformStartup<MongoConnectStartup>();
        services.AddPlatformStartup<MongoIndexStartup>();
        services.AddPlatformStartup<MongoMigrationStartup>();
        services.AddPlatformStartup<MongoSeedingStartup>();
        services.AddPlatformStartupGuard<IMongoDBProvider>();

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
