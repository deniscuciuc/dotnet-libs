using CoreLibs.Cache;
using CoreLibs.Redis;
using CoreLibs.Redis.Startup;
using CoreLibs.Startup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace CoreLibs.Redis.Integration.Tests;

public class RedisClientExtensionsTests
{
    [Fact]
    public void AddCoreRedis_WithConfig_RegistersRedisClient()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Redis:ConnectionString"] = "localhost:6379"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCoreRedis(config);

        var provider = services.BuildServiceProvider();
        var client = provider.GetService<IRedisClient>();

        Assert.NotNull(client);
    }

    [Fact]
    public void AddCoreRedis_WithDelegate_RegistersRedisClient()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCoreRedis(o => o.ConnectionString = "localhost:6379");

        var provider = services.BuildServiceProvider();
        var client = provider.GetService<IRedisClient>();

        Assert.NotNull(client);
    }

    [Fact]
    public void AddCoreRedisCache_RegistersICache()
    {
        var services = new ServiceCollection();
        var client = Substitute.For<IRedisClient>();
        services.AddSingleton(client);
        services.AddCoreRedisCache();

        var provider = services.BuildServiceProvider();
        var cache = provider.GetService<ICache>();

        Assert.NotNull(cache);
        Assert.IsType<RedisCache>(cache);
    }

    [Fact]
    public void AddCoreRedisStartup_RegistersStartupTasks()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCoreRedis(o => o.ConnectionString = "localhost:6379");
        services.AddCoreRedisStartup();

        var provider = services.BuildServiceProvider();
        var startups = provider.GetServices<IStartup>().ToList();

        Assert.Contains(startups, s => s is RedisConnectStartup);
    }
}
