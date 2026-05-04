using DenisCuciuc.Platform.Cache;
using DenisCuciuc.Platform.Redis;
using DenisCuciuc.Platform.Redis.Startup;
using DenisCuciuc.Platform.Startup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace DenisCuciuc.Platform.Redis.Integration.Tests;

public class RedisClientExtensionsTests
{
    [Fact]
    public void AddPlatformRedis_WithConfig_RegistersRedisClient()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Redis:ConnectionString"] = "localhost:6379"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPlatformRedis(config);

        var provider = services.BuildServiceProvider();
        var client = provider.GetService<IRedisClient>();

        Assert.NotNull(client);
    }

    [Fact]
    public void AddPlatformRedis_WithDelegate_RegistersRedisClient()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPlatformRedis(o => o.ConnectionString = "localhost:6379");

        var provider = services.BuildServiceProvider();
        var client = provider.GetService<IRedisClient>();

        Assert.NotNull(client);
    }

    [Fact]
    public void AddPlatformRedisCache_RegistersICache()
    {
        var services = new ServiceCollection();
        var client = Substitute.For<IRedisClient>();
        services.AddSingleton(client);
        services.AddPlatformRedisCache();

        var provider = services.BuildServiceProvider();
        var cache = provider.GetService<ICache>();

        Assert.NotNull(cache);
        Assert.IsType<RedisCache>(cache);
    }

    [Fact]
    public void AddPlatformRedisStartup_RegistersStartupTasks()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPlatformRedis(o => o.ConnectionString = "localhost:6379");
        services.AddPlatformRedisStartup();

        var provider = services.BuildServiceProvider();
        var startups = provider.GetServices<IStartup>().ToList();

        Assert.Contains(startups, s => s is RedisConnectStartup);
    }
}
