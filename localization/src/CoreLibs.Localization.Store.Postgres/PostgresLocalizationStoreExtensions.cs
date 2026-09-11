using CoreLibs.Postgres;
using CoreLibs.Startup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Npgsql;

namespace CoreLibs.Localization.Store.Postgres;

public static class PostgresLocalizationStoreExtensions
{
    /// <summary>
    /// Registers the Postgres localization store using raw Npgsql and Dapper.
    /// Requires <see cref="PostgresOptions"/> to be bound in DI via <c>AddCorePostgres</c>.
    /// </summary>
    public static IServiceCollection AddCoreLocalizationPostgres(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionPath = PostgresLocalizationStoreOptions.SectionPath)
    {
        services.Configure<PostgresLocalizationStoreOptions>(configuration.GetSection(sectionPath));
        return services.AddCoreServices();
    }

    /// <summary>
    /// Registers the Postgres localization store using raw Npgsql and Dapper.
    /// Requires <see cref="PostgresOptions"/> to be bound in DI via <c>AddCorePostgres</c>.
    /// </summary>
    public static IServiceCollection AddCoreLocalizationPostgres(
        this IServiceCollection services,
        string connectionString)
    {
        services.Configure<PostgresLocalizationStoreOptions>(_ => { });
        services.TryAddSingleton<NpgsqlDataSource>(_ => NpgsqlDataSource.Create(connectionString));
        return services.AddCoreServices();
    }

    private static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        services.TryAddSingleton<NpgsqlDataSource>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<PostgresOptions>>().Value;
            return NpgsqlDataSource.Create(opts.ConnectionString);
        });

        services.TryAddSingleton<PostgresLocalizationStore>();
        services.TryAddSingleton<ILocalizationStore>(sp => sp.GetRequiredService<PostgresLocalizationStore>());
        services.AddCoreStartup<PostgresLocalizationStoreStartup>();
        return services;
    }
}
