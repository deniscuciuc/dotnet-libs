using CoreLibs.LiveConfig.Redis;
using CoreLibs.Redis;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Testcontainers.Redis;

namespace CoreLibs.LiveConfig.IntegrationTests;

public class RedisConfigDistributorTests : IAsyncLifetime
{
    private readonly RedisContainer _container = new RedisBuilder("redis:7.0").Build();
    private RedisConfigDistributor _distributor = null!;
    private ConnectionMultiplexer _connection = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        _connection = await ConnectionMultiplexer.ConnectAsync(_container.GetConnectionString());
        _distributor = new RedisConfigDistributor(
            new TestRedisClient(_connection),
            Options.Create(new RedisLiveConfigOptions()),
            NullLogger<RedisConfigDistributor>.Instance);
    }

    public async Task DisposeAsync()
    {
        _connection.Dispose();
        await _container.DisposeAsync();
    }

    [Fact]
    public async Task SetAndGet_RoundTrip()
    {
        await _distributor.SetAsync("test", """{"data":1}""", 1);

        var entry = await _distributor.GetAsync("test");
        Assert.NotNull(entry);
        Assert.Equal("""{"data":1}""", entry.DataJson);
        Assert.Equal(1, entry.Version);
    }

    [Fact]
    public async Task Delete_RemovesEntry()
    {
        await _distributor.SetAsync("del", "{}", 1);
        await _distributor.DeleteAsync("del");

        var entry = await _distributor.GetAsync("del");
        Assert.Null(entry);
    }

    [Fact]
    public async Task GetManifest_ReturnsAllEntries()
    {
        await _distributor.SetAsync("m1", "{}", 1);
        await _distributor.SetAsync("m2", "{}", 2);

        var manifest = await _distributor.GetManifestAsync();
        Assert.Equal(2, manifest.Count);
        Assert.Equal(1, manifest["m1"]);
        Assert.Equal(2, manifest["m2"]);
    }

    [Fact]
    public async Task PublishAndSubscribe_ReceivesNotification()
    {
        var received = new TaskCompletionSource<ConfigUpdateNotification>();

        await _distributor.SubscribeAsync(notification =>
        {
            received.TrySetResult(notification);
            return Task.CompletedTask;
        });

        // Small delay to ensure subscription is established
        await Task.Delay(100);

        await _distributor.PublishUpdateAsync(new ConfigUpdateNotification("test", 5, false));

        var result = await received.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal("test", result.ConfigType);
        Assert.Equal(5, result.Version);
        Assert.False(result.IsRollback);
    }

    private sealed class TestRedisClient(ConnectionMultiplexer connection) : IRedisClient
    {
        public ConnectionMultiplexer Connection => connection;
        public IDatabase Database => connection.GetDatabase();
    }
}
