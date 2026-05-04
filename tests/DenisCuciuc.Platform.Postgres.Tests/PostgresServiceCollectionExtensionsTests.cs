using DenisCuciuc.Platform.Domain;
using DenisCuciuc.Platform.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace DenisCuciuc.Platform.Postgres.Tests;

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
    public void AddPlatformPostgres_WithConfig_RegistersDbContext()
    {
        var services = new ServiceCollection();
        services.AddPlatformPostgres<TestDbContext>(BuildConfig());

        var provider = services.BuildServiceProvider();
        var ctx = provider.GetRequiredService<TestDbContext>();

        Assert.NotNull(ctx);
    }

    [Fact]
    public void AddPlatformPostgres_WithDelegate_RegistersDbContext()
    {
        var services = new ServiceCollection();
        services.AddPlatformPostgres<TestDbContext>(o =>
        {
            o.ConnectionString = "Host=localhost;Database=delegatedb";
            o.MaxRetryCount = 1;
        });

        var provider = services.BuildServiceProvider();
        var ctx = provider.GetRequiredService<TestDbContext>();

        Assert.NotNull(ctx);
    }

    [Fact]
    public void AddPlatformPostgres_WithDomainEventDispatcher_RegistersInterceptor()
    {
        var services = new ServiceCollection();
        var dispatcher = Substitute.For<IDomainEventDispatcher>();
        services.AddSingleton(dispatcher);
        services.AddPlatformPostgres<TestDbContext>(BuildConfig());

        var provider = services.BuildServiceProvider();
        var ctx = provider.GetRequiredService<TestDbContext>();

        Assert.NotNull(ctx);
    }

    private class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
    }
}
