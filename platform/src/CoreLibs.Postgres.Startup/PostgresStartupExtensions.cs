using CoreLibs.Startup;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CoreLibs.Postgres.Startup;

public static class PostgresStartupExtensions
{
    /// <summary>
    /// Registers the PostgreSQL startup pipeline:
    /// <see cref="PostgresMigrateStartup{TContext}"/> (order 0)
    /// and a <see cref="StartupGuard{T}"/> for <typeparamref name="TContext"/>.
    /// </summary>
    public static IServiceCollection AddCorePostgresStartup<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        services.AddCoreStartup<PostgresMigrateStartup<TContext>>();
        services.AddCoreStartupGuard<TContext>();
        return services;
    }

    /// <summary>
    /// Marks the <see cref="StartupGuard{T}"/> for <typeparamref name="TContext"/> as configured.
    /// Called automatically by <see cref="PostgresMigrateStartup{TContext}"/>; only use this if you are
    /// doing fully manual orchestration.
    /// </summary>
    public static void MarkPostgresConfigured<TContext>(this IHostStartup hostStartup)
        where TContext : DbContext =>
        hostStartup.MarkConfigured<TContext>();
}
