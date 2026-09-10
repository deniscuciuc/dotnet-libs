using CoreLibs.Domain;
using CoreLibs.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace CoreLibs.Postgres.Tests;

public class PostgresServiceCollectionExtensionsTests
{
    private static IConfiguration BuildConfig(string connectionString = "Host=localhost;Database=testdb") =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Postgres:ConnectionString"] = connectionString
            })
            .Build();

    [Fact]
    public void AddCorePostgres_WithConfig_RegistersDbContext()
    {
        var services = new ServiceCollection();
        services.AddCorePostgres<TestDbContext>(BuildConfig());

        var provider = services.BuildServiceProvider();
        var ctx = provider.GetRequiredService<TestDbContext>();

        Assert.NotNull(ctx);
    }

    [Fact]
    public void AddCorePostgres_WithDelegate_RegistersDbContext()
    {
        var services = new ServiceCollection();
        services.AddCorePostgres<TestDbContext>(o =>
        {
            o.ConnectionString = "Host=localhost;Database=delegatedb";
            o.MaxRetryCount = 1;
        });

        var provider = services.BuildServiceProvider();
        var ctx = provider.GetRequiredService<TestDbContext>();

        Assert.NotNull(ctx);
    }

    [Fact]
    public void AddCorePostgres_WithDomainEventDispatcher_RegistersInterceptor()
    {
        var services = new ServiceCollection();
        var dispatcher = Substitute.For<IDomainEventDispatcher>();
        services.AddSingleton(dispatcher);
        services.AddCorePostgres<TestDbContext>(BuildConfig());

        var provider = services.BuildServiceProvider();
        var ctx = provider.GetRequiredService<TestDbContext>();

        Assert.NotNull(ctx);
    }

    private class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
    }
}
