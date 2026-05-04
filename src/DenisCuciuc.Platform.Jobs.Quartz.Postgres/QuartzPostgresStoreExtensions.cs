using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace DenisCuciuc.Platform.Jobs.Quartz.Postgres;

public static class QuartzPostgresStoreExtensions
{
    private const string DefaultPostgresSectionPath = "Postgres";

    /// <summary>
    /// Configures Quartz.NET to use PostgreSQL as the persistent job store (AdoJobStore).
    /// Reads connection string from the <c>Postgres</c> configuration section.
    /// Called via reflection from <see cref="JobsServiceCollectionExtensions"/>.
    /// </summary>
    public static IServiceCollection AddQuartzPostgresStore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetSection(DefaultPostgresSectionPath)["ConnectionString"]
                               ?? throw new InvalidOperationException(
                                   $"PostgreSQL connection string not found at '{DefaultPostgresSectionPath}:ConnectionString'. " +
                                   "Ensure DenisCuciuc.Platform.Postgres is configured.");

        // Apply schema migration synchronously so all tables exist before
        // the Quartz scheduler starts and performs its schema validation.
        QuartzPostgresSchemaGenerator.Apply(connectionString);

        services.AddQuartz(q =>
        {
            q.UsePersistentStore(store =>
            {
                store.UseProperties = true;
                store.RetryInterval = TimeSpan.FromSeconds(15);

                store.UsePostgres(pg =>
                {
                    pg.ConnectionString = connectionString;
                    pg.TablePrefix = QuartzPostgresSchemaGenerator.TablePrefix;
                });

                store.UseSystemTextJsonSerializer();

                store.UseClustering(c =>
                {
                    c.CheckinMisfireThreshold = TimeSpan.FromSeconds(20);
                    c.CheckinInterval = TimeSpan.FromSeconds(10);
                });
            });
        });

        return services;
    }
}
