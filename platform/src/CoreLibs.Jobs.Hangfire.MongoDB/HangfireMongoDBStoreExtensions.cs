using Hangfire;
using Hangfire.Mongo;
using Hangfire.Mongo.Migration.Strategies;
using Hangfire.Mongo.Migration.Strategies.Backup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace CoreLibs.Jobs.Hangfire.MongoDB;

public static class HangfireMongoDBStoreExtensions
{
    private const string DefaultMongoSectionPath = "MongoDB";

    /// <summary>
    /// Configures Hangfire to use MongoDB as the persistent job store.
    /// Reads connection string from the <c>MongoDB</c> configuration section.
    /// Called via reflection from <see cref="JobsServiceCollectionExtensions"/>.
    /// </summary>
    public static IServiceCollection AddHangfireMongoDBStore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetSection(DefaultMongoSectionPath)["ConnectionString"]
                               ?? throw new InvalidOperationException(
                                   $"MongoDB connection string not found at '{DefaultMongoSectionPath}:ConnectionString'. " +
                                   "Ensure CoreLibs.MongoDB is configured.");

        var mongoUrl = new MongoUrl(connectionString);
        var databaseName = mongoUrl.DatabaseName
                           ?? throw new InvalidOperationException(
                               "MongoDB connection string must include a database name.");

        var mongoClient = new MongoClient(mongoUrl);

        var queueStrategy = configuration.GetSection(CoreJobsOptions.DefaultSectionPath)["mongoCheckQueuedJobsStrategy"] switch
        {
            "Watch" => CheckQueuedJobsStrategy.Watch,
            "Poll" => CheckQueuedJobsStrategy.Poll,
            _ => CheckQueuedJobsStrategy.TailNotificationsCollection
        };

        services.AddHangfire((_, config) =>
        {
            config.UseMongoStorage(mongoClient, databaseName, new MongoStorageOptions
            {
                Prefix = "hangfire",
                CheckConnection = true,
                CheckQueuedJobsStrategy = queueStrategy,
                MigrationOptions = new MongoMigrationOptions
                {
                    MigrationStrategy = new MigrateMongoMigrationStrategy(),
                    BackupStrategy = new CollectionMongoBackupStrategy()
                }
            });
        });

        return services;
    }
}
