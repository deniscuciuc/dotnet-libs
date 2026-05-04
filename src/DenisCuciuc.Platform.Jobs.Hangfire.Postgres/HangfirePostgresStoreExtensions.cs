using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DenisCuciuc.Platform.Jobs.Hangfire.Postgres;

public static class HangfirePostgresStoreExtensions
{
    private const string DefaultPostgresSectionPath = "Postgres";

    /// <summary>
    /// Configures Hangfire to use PostgreSQL as the persistent job store.
    /// Reads connection string from the <c>Postgres</c> configuration section.
    /// Called via reflection from <see cref="JobsServiceCollectionExtensions"/>.
    /// </summary>
    public static IServiceCollection AddHangfirePostgresStore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetSection(DefaultPostgresSectionPath)["ConnectionString"]
                               ?? throw new InvalidOperationException(
                                   $"PostgreSQL connection string not found at '{DefaultPostgresSectionPath}:ConnectionString'. " +
                                   "Ensure DenisCuciuc.Platform.Postgres is configured.");

        services.AddHangfire((_, config) =>
        {
            config.UsePostgreSqlStorage(o => { o.UseNpgsqlConnection(connectionString); });
        });

        return services;
    }
}
