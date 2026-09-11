using CoreLibs.Postgres;
using CoreLibs.Postgres.Startup;
using CoreLibs.Startup;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CoreLibs.Postgres.Tests;

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
    public void AddCorePostgresStartup_RegistersStartupTasks()
    {
        var services = new ServiceCollection();
        services.AddCorePostgres<TestDbContext>(BuildConfig());
        services.AddCorePostgresStartup<TestDbContext>();

        var provider = services.BuildServiceProvider();
        var startups = provider.GetServices<IStartup>().ToList();

        // Should have PostgresMigrateStartup + StartupGuard<TestDbContext>
        Assert.Equal(2, startups.Count);
    }

    [Fact]
    public void AddCorePostgresStartup_RegistersStartupGuard()
    {
        var services = new ServiceCollection();
        services.AddCorePostgres<TestDbContext>(BuildConfig());
        services.AddCorePostgresStartup<TestDbContext>();

        var provider = services.BuildServiceProvider();
        var guard = provider.GetService<StartupGuard<TestDbContext>>();

        Assert.NotNull(guard);
    }

    private class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
    }
}
