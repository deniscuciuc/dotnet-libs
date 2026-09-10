using CoreLibs.LiveConfig.Startup;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace CoreLibs.LiveConfig.UnitTests;

public class LiveConfigPreloaderTests
{
    private readonly IConfigStore _store = Substitute.For<IConfigStore>();
    private readonly ConfigApplierRegistry _applierRegistry = new();
    private readonly ConfigHookRegistry _hookRegistry = new();

    private LiveConfigEngine CreateEngine(IConfigDistributor? distributor = null)
    {
        return new LiveConfigEngine(_store, _applierRegistry, _hookRegistry,
            NullLogger<LiveConfigEngine>.Instance, distributor);
    }

    [Fact]
    public async Task PreloadAsync_UsesActiveConfig_WhenAvailable()
    {
        var version = new ConfigVersion("bonuses", 3, "[1,2,3]", "hash", true, DateTimeOffset.UtcNow, null);
        _store.GetActiveAsync("bonuses", Arg.Any<CancellationToken>()).Returns(version);

        var applier = Substitute.For<IConfigApplier>();
        applier.ConfigType.Returns("bonuses");
        _applierRegistry.Register(applier);

        var source = new TestConfigSource("gsheet");
        var registry = new ConfigSourceRegistry();
        registry.Register(source);

        var preloader = new LiveConfigPreloader(CreateEngine(), registry, NullLogger<LiveConfigPreloader>.Instance);

        await preloader.PreloadAsync(["bonuses"], new RetryOptions { MaxRetries = 0 });

        await applier.Received().ApplyAsync("[1,2,3]", 3, Arg.Any<CancellationToken>());
        Assert.Empty(source.FetchRequests);
        await _store.DidNotReceive().CreateVersionAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PreloadAsync_BootstrapsFromSingleSource_WhenActiveConfigMissing()
    {
        _store.GetActiveAsync("bonuses", Arg.Any<CancellationToken>()).Returns((ConfigVersion?)null);
        _store.GetActiveHashAsync("bonuses", Arg.Any<CancellationToken>()).Returns((string?)null);
        _store.CreateVersionAsync("bonuses", Arg.Any<string>(), Arg.Any<string>(), "startup-preload",
                Arg.Any<CancellationToken>())
            .Returns(1);
        _store.ActivateVersionAsync("bonuses", 1, Arg.Any<CancellationToken>()).Returns(true);

        var applier = Substitute.For<IConfigApplier>();
        applier.ConfigType.Returns("bonuses");
        _applierRegistry.Register(applier);

        var source = new TestConfigSource("gsheet");
        source.AddSnapshot("bonuses", "[{\"name\":\"Welcome Bonus\"}]");

        var registry = new ConfigSourceRegistry();
        registry.Register(source);

        var preloader = new LiveConfigPreloader(CreateEngine(), registry, NullLogger<LiveConfigPreloader>.Instance);

        await preloader.PreloadAsync(["bonuses"], new RetryOptions { MaxRetries = 0 });

        Assert.Equal(["bonuses"], source.FetchRequests);
        await _store.Received().CreateVersionAsync(
            "bonuses", "[{\"name\":\"Welcome Bonus\"}]", Arg.Any<string>(), "startup-preload",
            Arg.Any<CancellationToken>());
        await applier.Received().ApplyAsync("[{\"name\":\"Welcome Bonus\"}]", 1, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PreloadAsync_Throws_WhenMultipleSourcesRegisteredWithoutSourceName()
    {
        _store.GetActiveAsync("bonuses", Arg.Any<CancellationToken>()).Returns((ConfigVersion?)null);

        var registry = new ConfigSourceRegistry();
        registry.Register(new TestConfigSource("gsheet"));
        registry.Register(new TestConfigSource("json-file"));

        var preloader = new LiveConfigPreloader(CreateEngine(), registry, NullLogger<LiveConfigPreloader>.Instance);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            preloader.PreloadAsync(["bonuses"], new RetryOptions { MaxRetries = 0 }));

        Assert.Contains("Multiple config sources are registered", ex.Message);
        Assert.Contains("preloadSourceName", ex.Message);
    }

    private sealed class TestConfigSource(string sourceName) : IConfigSource
    {
        private readonly Dictionary<string, ConfigSnapshot> _snapshots = new(StringComparer.OrdinalIgnoreCase);

        public string SourceName => sourceName;

        public List<string> FetchRequests { get; } = [];

        public void AddSnapshot(string configType, string dataJson)
        {
            _snapshots[configType] = new ConfigSnapshot(
                configType,
                dataJson,
                ConfigHasher.ComputeHash(dataJson),
                DateTimeOffset.UtcNow);
        }

        public Task<ConfigSnapshot> FetchAsync(string configType, CancellationToken cancellationToken = default)
        {
            FetchRequests.Add(configType);
            return Task.FromResult(_snapshots[configType]);
        }

        public Task<IReadOnlyDictionary<string, ConfigSnapshot>> FetchAllAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyDictionary<string, ConfigSnapshot>>(_snapshots);
        }
    }
}
