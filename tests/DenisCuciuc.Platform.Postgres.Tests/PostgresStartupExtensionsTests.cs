using DenisCuciuc.Platform.Postgres;
using DenisCuciuc.Platform.Postgres.Startup;
using DenisCuciuc.Platform.Startup;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DenisCuciuc.Platform.Postgres.Tests;

public class PostgresStartupExtensionsTests
{
    private static IConfiguration BuildConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Postgres:ConnectionString"] = "Host=localhost;Database=testdb"
            })
            .Build();

    [Fact]
    public void AddPlatformPostgresStartup_RegistersStartupTasks()
    {
        var services = new ServiceCollection();
        services.AddPlatformPostgres<TestDbContext>(BuildConfig());
        services.AddPlatformPostgresStartup<TestDbContext>();

        var provider = services.BuildServiceProvider();
        var startups = provider.GetServices<IStartup>().ToList();

        // Should have PostgresMigrateStartup + StartupGuard<TestDbContext>
        Assert.Equal(2, startups.Count);
    }

    [Fact]
    public void AddPlatformPostgresStartup_RegistersStartupGuard()
    {
        var services = new ServiceCollection();
        services.AddPlatformPostgres<TestDbContext>(BuildConfig());
        services.AddPlatformPostgresStartup<TestDbContext>();

        var provider = services.BuildServiceProvider();
        var guard = provider.GetService<StartupGuard<TestDbContext>>();

        Assert.NotNull(guard);
    }

    private class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
    }
}
