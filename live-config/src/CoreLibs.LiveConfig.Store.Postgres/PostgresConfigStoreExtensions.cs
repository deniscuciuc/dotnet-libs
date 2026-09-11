using CoreLibs.Postgres;
using CoreLibs.Startup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Npgsql;

namespace CoreLibs.LiveConfig.Store.Postgres;

public static class PostgresConfigStoreExtensions
{
    /// <summary>
    /// Registers the Postgres config store using raw Npgsql and Dapper.
    /// Reads store options from <paramref name="configuration"/> at <paramref name="sectionPath"/>.
    /// Requires <see cref="PostgresOptions"/> to be bound in DI via <c>AddCorePostgres</c>.
    /// </summary>
    public static IServiceCollection AddLiveConfigPostgresStore(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionPath = PostgresConfigStoreOptions.SectionPath)
    {
        services.Configure<PostgresConfigStoreOptions>(configuration.GetSection(sectionPath));
        return services.AddCoreServices();
    }

    /// <summary>
    /// Registers the Postgres config store using raw Npgsql and Dapper.
    /// Requires <see cref="PostgresOptions"/> to be bound in DI via <c>AddCorePostgres</c>.
    /// </summary>
    public static IServiceCollection AddLiveConfigPostgresStore(
        this IServiceCollection services,
        Action<PostgresConfigStoreOptions>? configure = null)
    {
        if (configure is not null)
            services.Configure(configure);
        else
            services.Configure<PostgresConfigStoreOptions>(_ => { });
        return services.AddCoreServices();
    }

    private static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        // Register NpgsqlDataSource from PostgresOptions if not already present.
        services.TryAddSingleton<NpgsqlDataSource>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<PostgresOptions>>().Value;
            return NpgsqlDataSource.Create(opts.ConnectionString);
        });

        services.TryAddSingleton<PostgresConfigStore>();
        services.TryAddSingleton<IConfigStore>(sp => sp.GetRequiredService<PostgresConfigStore>());
        services.AddCoreStartup<PostgresConfigStoreStartup>();
        return services;
    }
}
