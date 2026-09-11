using CoreLibs.Startup;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CoreLibs.Postgres.Startup;

/// <summary>
/// Applies pending EF Core migrations for <typeparamref name="TContext"/> at startup.
/// </summary>
public sealed class PostgresMigrateStartup<TContext> : IStartup where TContext : DbContext
{
    public int Order => 0;

    public async Task StartupAsync(IHostStartup host, CancellationToken cancellationToken = default)
    {
        host.Logger.LogInformation("Applying PostgreSQL migrations for {Context} ...", typeof(TContext).Name);

        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TContext>();
        await db.Database.MigrateAsync(cancellationToken);

        host.MarkConfigured<TContext>();

        host.Logger.LogInformation("PostgreSQL migrations applied for {Context}", typeof(TContext).Name);
    }
}
