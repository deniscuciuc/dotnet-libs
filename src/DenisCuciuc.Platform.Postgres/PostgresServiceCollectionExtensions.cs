using DenisCuciuc.Platform.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace DenisCuciuc.Platform.Postgres;

public static class PostgresServiceCollectionExtensions
{
    /// <summary>
    /// Registers a <typeparamref name="TContext"/> backed by PostgreSQL using the <c>Postgres</c>
    /// configuration section. Domain events are dispatched automatically after SaveChanges when
    /// <see cref="DomainExtensions.AddPlatformDomainEvents"/> has been called.
    /// </summary>
    public static IServiceCollection AddPlatformPostgres<TContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionPath = PostgresOptions.DefaultSectionPath,
        Action<NpgsqlDataSourceBuilder>? configureDataSource = null,
        Action<DbContextOptionsBuilder>? configureDbContext = null)
        where TContext : DbContext
    {
        services
            .AddOptions<PostgresOptions>()
            .Bind(configuration.GetSection(sectionPath))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var options = configuration.GetSection(sectionPath).Get<PostgresOptions>() ?? new PostgresOptions();

        services.AddDbContext<TContext>((sp, dbOptions) =>
        {
            ConfigureNpgsql(dbOptions, options, configureDataSource);
            ApplyNamingConvention(dbOptions, options.NamingConvention);

            if (options.EnableSensitiveDataLogging)
                dbOptions.EnableSensitiveDataLogging();

            if (options.EnableDetailedErrors)
                dbOptions.EnableDetailedErrors();

            var dispatcher = sp.GetService<IDomainEventDispatcher>();
            if (dispatcher is not null)
                dbOptions.AddInterceptors(new DomainEventSaveChangesInterceptor(dispatcher));

            configureDbContext?.Invoke(dbOptions);
        });

        return services;
    }

    /// <summary>
    /// Registers a <typeparamref name="TContext"/> backed by PostgreSQL using an explicit options delegate.
    /// </summary>
    public static IServiceCollection AddPlatformPostgres<TContext>(
        this IServiceCollection services,
        Action<PostgresOptions> configureOptions,
        Action<NpgsqlDataSourceBuilder>? configureDataSource = null,
        Action<DbContextOptionsBuilder>? configureDbContext = null)
        where TContext : DbContext
    {
        services
            .AddOptions<PostgresOptions>()
            .Configure(configureOptions)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var options = new PostgresOptions();
        configureOptions(options);

        services.AddDbContext<TContext>((sp, dbOptions) =>
        {
            ConfigureNpgsql(dbOptions, options, configureDataSource);
            ApplyNamingConvention(dbOptions, options.NamingConvention);

            if (options.EnableSensitiveDataLogging)
                dbOptions.EnableSensitiveDataLogging();

            if (options.EnableDetailedErrors)
                dbOptions.EnableDetailedErrors();

            var dispatcher = sp.GetService<IDomainEventDispatcher>();
            if (dispatcher is not null)
                dbOptions.AddInterceptors(new DomainEventSaveChangesInterceptor(dispatcher));

            configureDbContext?.Invoke(dbOptions);
        });

        return services;
    }

    private static void ApplyNamingConvention(DbContextOptionsBuilder dbOptions, PostgresNamingConvention convention)
    {
        switch (convention)
        {
            case PostgresNamingConvention.SnakeCase:
                dbOptions.UseSnakeCaseNamingConvention();
                break;
            case PostgresNamingConvention.LowerCase:
                dbOptions.UseLowerCaseNamingConvention();
                break;
            case PostgresNamingConvention.CamelCase:
                dbOptions.UseCamelCaseNamingConvention();
                break;
        }
    }

    private static void ConfigureNpgsql(
        DbContextOptionsBuilder dbOptions,
        PostgresOptions options,
        Action<NpgsqlDataSourceBuilder>? configureDataSource)
    {
        void NpgsqlOptions(NpgsqlDbContextOptionsBuilder npgsql)
        {
            npgsql.CommandTimeout(options.CommandTimeout);
            npgsql.EnableRetryOnFailure(options.MaxRetryCount);
        }

        if (options.EnableDynamicJson || configureDataSource is not null)
        {
            var builder = new NpgsqlDataSourceBuilder(options.ConnectionString);
            if (options.EnableDynamicJson)
                builder.EnableDynamicJson();
            configureDataSource?.Invoke(builder);
            dbOptions.UseNpgsql(builder.Build(), NpgsqlOptions);
        }
        else
        {
            dbOptions.UseNpgsql(options.ConnectionString, NpgsqlOptions);
        }
    }

    /// <summary>
    /// Adds a PostgreSQL health check using the configured <see cref="PostgresOptions.ConnectionString"/>.
    /// </summary>
    public static IHealthChecksBuilder AddPostgresHealthCheck(this IHealthChecksBuilder builder)
    {
        builder.AddNpgSql(sp =>
            sp.GetRequiredService<IOptions<PostgresOptions>>().Value.ConnectionString);
        return builder;
    }
}
