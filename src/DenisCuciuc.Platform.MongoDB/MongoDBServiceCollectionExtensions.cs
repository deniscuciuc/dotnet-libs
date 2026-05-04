using System.Diagnostics.Contracts;
using DenisCuciuc.Platform.Cache;
using DenisCuciuc.Platform.MongoDB.Actor;
using DenisCuciuc.Platform.MongoDB.Cache;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;

namespace DenisCuciuc.Platform.MongoDB;

public static class MongoDBServiceCollectionExtensions
{
    public static IServiceCollection AddMongoDB(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionPath = MongoDBOptions.DefaultSectionPath,
        Func<MongoClientSettings, (IMongoDBProvider, IMongoDBConnection)>? factory = null)
    {
        var options = new MongoDBOptions();
        configuration.GetSection(sectionPath).Bind(options);
        return services.AddMongoDBCore(options, factory);
    }

    public static IServiceCollection AddMongoDB(
        this IServiceCollection services,
        MongoDBOptions options,
        Func<MongoClientSettings, (IMongoDBProvider, IMongoDBConnection)>? factory = null)
    {
        return services.AddMongoDBCore(options, factory);
    }

    private static IServiceCollection AddMongoDBCore(
        this IServiceCollection services,
        MongoDBOptions options,
        Func<MongoClientSettings, (IMongoDBProvider, IMongoDBConnection)>? factory)
    {
        GlobalInitializer.Setup();

        var url = new MongoUrl(options.ConnectionString);

        var settings = MongoClientSettings.FromUrl(url);

        settings.MinConnectionPoolSize = options.MinConnectionPoolSize;
        settings.MaxConnectionPoolSize = options.MaxConnectionPoolSize;
        settings.WaitQueueTimeout = options.WaitQueueTimeout;

        (IMongoDBProvider Provider, IMongoDBConnection Connection) client;
        if (factory != null)
        {
            client = factory(settings);
        }
        else
        {
            var databaseName = url.DatabaseName
                ?? throw new InvalidOperationException(
                    "MongoDB database name must be specified in the connection string " +
                    "(e.g. mongodb://localhost:27017/mydb).");
            var instance = new MongoDBProvider(databaseName, settings);
            client = (instance, instance);
        }

        services.AddSingleton<IMongoDBProvider>(_ => client.Provider);
        services.AddSingleton(client.GetType(), _ => client);
        services.AddSingleton<IMongoDBConnection>(_ => client.Connection);

        return services;
    }

    public static IServiceCollection AddMongoDBCache(this IServiceCollection services)
    {
        services.AddSingleton<MongoCache>();
        services.AddSingleton<ICache>(s => s.GetRequiredService<MongoCache>());
        return services;
    }

    public static IServiceCollection AddRepository<T>(this IServiceCollection services)
        where T : class, IRepository
    {
        Contract.Assert(!typeof(T).IsInterface, $"{typeof(T)} must NOT be an interface");
        Contract.Assert(!typeof(T).IsAbstract, $"{typeof(T)} must NOT be an abstract class");

        var api = typeof(T).GetInterfaces();

        services.AddSingleton<T>();
        foreach (var type in api) services.AddSingleton(type, sp => sp.GetRequiredService<T>());

        return services;
    }

    public static IServiceCollection AddRepository<TApi, TImpl>(this IServiceCollection services)
        where TApi : class
        where TImpl : class, IRepository, TApi
    {
        Contract.Assert(!typeof(TImpl).IsInterface, $"{typeof(TImpl)} must NOT be an interface");
        Contract.Assert(!typeof(TImpl).IsAbstract, $"{typeof(TImpl)} must NOT be an abstract class");

        services.AddSingleton<TImpl>();
        services.AddSingleton<TApi>(x => x.GetRequiredService<TImpl>());
        services.AddSingleton<IRepository>(x => x.GetRequiredService<TImpl>());

        if (typeof(IRepositoryApplyIndex).IsAssignableFrom(typeof(TImpl)))
            services.AddSingleton<IRepositoryApplyIndex>(x => (IRepositoryApplyIndex)x.GetRequiredService<TImpl>());

        return services;
    }

    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        var repositoryBaseType = typeof(IRepository);
        var types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type =>
                type is { IsClass: true, IsAbstract: false, IsInterface: false }
                && repositoryBaseType.IsAssignableFrom(type)
            );

        foreach (var impl in types)
        {
            var api = impl.GetInterfaces();
            services.AddSingleton(impl);

            foreach (var type in api) services.AddSingleton(type, sp => sp.GetRequiredService(impl));
        }

        return services;
    }

    public static ActorRepositoryBuilder<TRepo, TEntity, TActor, TActorResult>
        AddActorRepository<TRepo, TEntity, TActor, TActorResult>(this IServiceCollection services)
        where TRepo : class, IActorRepository<TEntity, TActor, TActorResult>
        where TEntity : EntityCas
        where TActor : Actor<TEntity>, TActorResult
        where TActorResult : IActorResult<TEntity>
    {
        // Register default in-memory cache unless consumer overrides via builder
        services.TryAddSingleton<IEntityCache<TEntity>, MemoryEntityCache<TEntity>>();

        // Register the repo itself + its interfaces
        services.AddSingleton<TRepo>();
        foreach (var iface in typeof(TRepo).GetInterfaces())
            services.AddSingleton(iface, sp => sp.GetRequiredService<TRepo>());

        services.AddSingleton<IRepository>(sp => sp.GetRequiredService<TRepo>());

        if (typeof(IRepositoryApplyIndex).IsAssignableFrom(typeof(TRepo)))
            services.AddSingleton<IRepositoryApplyIndex>(sp => (IRepositoryApplyIndex)sp.GetRequiredService<TRepo>());

        return new ActorRepositoryBuilder<TRepo, TEntity, TActor, TActorResult>(services);
    }

    public static IHealthChecksBuilder AddMongoDBHealthCheck(this IHealthChecksBuilder builder)
    {
        builder.AddMongoDb(sp => sp.GetRequiredService<IMongoDBProvider>().Client);
        return builder;
    }
}
