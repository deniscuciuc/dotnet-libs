using CoreLibs.MongoDB;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CoreLibs.LiveConfig.Store.MongoDB;

public static class MongoConfigStoreExtensions
{
    /// <summary>
    /// Registers the MongoDB config store with options from configuration.
    /// Requires <see cref="IMongoDBProvider"/> to be registered (via <c>AddMongoDB()</c>).
    /// Indexes are applied automatically during <c>RunMongoDBAsync()</c>.
    /// </summary>
    public static IServiceCollection AddLiveConfigMongoStore(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionPath = MongoConfigStoreOptions.SectionPath)
    {
        services.Configure<MongoConfigStoreOptions>(configuration.GetSection(sectionPath));
        RegisterMongoStore(services);
        return services;
    }

    /// <summary>
    /// Registers the MongoDB config store with explicit options.
    /// </summary>
    public static IServiceCollection AddLiveConfigMongoStore(
        this IServiceCollection services,
        Action<MongoConfigStoreOptions>? configure = null)
    {
        if (configure is not null)
            services.Configure(configure);
        else
            services.Configure<MongoConfigStoreOptions>(_ => { });

        RegisterMongoStore(services);
        return services;
    }

    private static void RegisterMongoStore(IServiceCollection services)
    {
        services.AddSingleton<MongoConfigSnapshotIndexes>();
        services.AddSingleton<MongoConfigStore>();
        services.AddSingleton<IConfigStore>(sp => sp.GetRequiredService<MongoConfigStore>());
        services.AddSingleton<IRepositoryApplyIndex>(sp => sp.GetRequiredService<MongoConfigStore>());
    }
}
