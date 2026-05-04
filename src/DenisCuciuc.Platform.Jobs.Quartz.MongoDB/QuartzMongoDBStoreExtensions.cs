using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Quartz;
using Reddoxx.Quartz.MongoDbJobStore;
using Reddoxx.Quartz.MongoDbJobStore.Database;
using Reddoxx.Quartz.MongoDbJobStore.Extensions;

namespace DenisCuciuc.Platform.Jobs.Quartz.MongoDB;

public static class QuartzMongoDBStoreExtensions
{
    private const string DefaultMongoSectionPath = "MongoDB";

    /// <summary>
    /// Configures Quartz.NET to use MongoDB as the persistent job store
    /// via <c>Reddoxx.Quartz.MongoDbJobStore</c>.
    /// Reads connection string from the <c>MongoDB</c> configuration section.
    /// Called via reflection from <see cref="JobsServiceCollectionExtensions"/>.
    /// </summary>
    public static IServiceCollection AddQuartzMongoDBStore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetSection(DefaultMongoSectionPath)["ConnectionString"]
                               ?? throw new InvalidOperationException(
                                   $"MongoDB connection string not found at '{DefaultMongoSectionPath}:ConnectionString'. " +
                                   "Ensure DenisCuciuc.Platform.MongoDB is configured.");

        var mongoUrl = new MongoUrl(connectionString);
        var databaseName = mongoUrl.DatabaseName
                           ?? throw new InvalidOperationException(
                               "MongoDB connection string must include a database name.");

        // Register the job store factory for Reddoxx.Quartz.MongoDbJobStore
        services.AddSingleton<IQuartzMongoDbJobStoreFactory>(
            _ => new PlatformQuartzMongoDbJobStoreFactory(connectionString, databaseName));

        services.AddQuartz(q =>
        {
            q.UsePersistentStore<MongoDbJobStore>(storage =>
            {
                storage.UseClustering();
                storage.UseSystemTextJsonSerializer();

                storage.ConfigureMongoDb(c =>
                {
                    c.CollectionPrefix = "quartz";
                });
            });
        });

        return services;
    }
}

/// <summary>
/// Provides the MongoDB database instance to <see cref="MongoDbJobStore"/>.
/// </summary>
internal sealed class PlatformQuartzMongoDbJobStoreFactory : IQuartzMongoDbJobStoreFactory
{
    private readonly IMongoDatabase _database;

    public PlatformQuartzMongoDbJobStoreFactory(string connectionString, string databaseName)
    {
        var client = new MongoClient(connectionString);
        _database = client.GetDatabase(databaseName);
    }

    public IMongoDatabase GetDatabase() => _database;
}
