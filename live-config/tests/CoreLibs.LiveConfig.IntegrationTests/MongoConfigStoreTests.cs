using CoreLibs.LiveConfig.Store.MongoDB;
using CoreLibs.MongoDB;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Testcontainers.MongoDb;

namespace CoreLibs.LiveConfig.IntegrationTests;

public class MongoConfigStoreTests : IAsyncLifetime
{
    private readonly MongoDbContainer _container = new MongoDbBuilder("mongo:6.0").Build();
    private MongoConfigStore _store = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        var client = new MongoClient(_container.GetConnectionString());
        var provider = new TestMongoDBProvider(client, client.GetDatabase("liveconfig_test"));
        _store = new MongoConfigStore(
            provider,
            new MongoConfigSnapshotIndexes(),
            Options.Create(new MongoConfigStoreOptions()),
            NullLogger<MongoConfigStore>.Instance);
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    [Fact]
    public async Task CreateVersion_And_GetActive_RoundTrip()
    {
        var version = await _store.CreateVersionAsync("test", """{"key":"value"}""", "abc123");
        Assert.Equal(1, version);

        var activated = await _store.ActivateVersionAsync("test", version);
        Assert.True(activated);

        var active = await _store.GetActiveAsync("test");
        Assert.NotNull(active);
        Assert.Equal(1, active.Version);
        Assert.Equal("test", active.ConfigType);
        Assert.True(active.IsActive);
    }

    [Fact]
    public async Task GetRecentVersions_ReturnsDescending()
    {
        await _store.CreateVersionAsync("ordered", "{}", "h1");
        await _store.CreateVersionAsync("ordered", "{}", "h2");
        await _store.CreateVersionAsync("ordered", "{}", "h3");

        var versions = await _store.GetRecentVersionsAsync("ordered", 10);
        Assert.Equal(3, versions.Count);
        Assert.Equal(3, versions[0].Version);
        Assert.Equal(1, versions[^1].Version);
    }

    [Fact]
    public async Task ActivateVersion_SwitchesActive()
    {
        await _store.CreateVersionAsync("switch", """{"v":1}""", "h1");
        await _store.CreateVersionAsync("switch", """{"v":2}""", "h2");
        await _store.ActivateVersionAsync("switch", 1);

        var active = await _store.GetActiveAsync("switch");
        Assert.Equal(1, active!.Version);

        await _store.ActivateVersionAsync("switch", 2);
        active = await _store.GetActiveAsync("switch");
        Assert.Equal(2, active!.Version);
    }

    [Fact]
    public async Task GetManifest_ReturnsActiveVersions()
    {
        await _store.CreateVersionAsync("alpha", "{}", "ha");
        await _store.ActivateVersionAsync("alpha", 1);
        await _store.CreateVersionAsync("beta", "{}", "hb");
        await _store.ActivateVersionAsync("beta", 1);

        var manifest = await _store.GetManifestAsync();
        Assert.Equal(2, manifest.Count);
        Assert.Equal(1, manifest["alpha"]);
        Assert.Equal(1, manifest["beta"]);
    }

    private sealed class TestMongoDBProvider(IMongoClient client, IMongoDatabase database) : IMongoDBProvider
    {
        public IMongoClient Client => client;
        public IMongoDatabase Database => database;
    }
}
