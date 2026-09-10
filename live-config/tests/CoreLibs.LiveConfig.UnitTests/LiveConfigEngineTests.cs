using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace CoreLibs.LiveConfig.UnitTests;

public class LiveConfigEngineTests
{
    private readonly IConfigStore _store = Substitute.For<IConfigStore>();
    private readonly ConfigApplierRegistry _applierRegistry = new();
    private readonly ConfigHookRegistry _hookRegistry = new();
    private readonly NullLogger<LiveConfigEngine> _logger = NullLogger<LiveConfigEngine>.Instance;

    private LiveConfigEngine CreateEngine(IConfigDistributor? distributor = null)
    {
        return new LiveConfigEngine(_store, _applierRegistry, _hookRegistry, _logger, distributor);
    }

    [Fact]
    public async Task ImportAsync_NewData_CreatesVersionAndActivates()
    {
        _store.GetActiveHashAsync("test", Arg.Any<CancellationToken>()).Returns((string?)null);
        _store.CreateVersionAsync("test", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(1);
        _store.ActivateVersionAsync("test", 1, Arg.Any<CancellationToken>()).Returns(true);

        var engine = CreateEngine();
        var data = new[] { new { Id = 1 } };
        var result = await engine.ImportAsync("test", data);

        Assert.True(result.Changed);
        Assert.Equal(1, result.Version);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ImportAsync_SameHash_SkipsImport()
    {
        var data = new[] { new { Id = 1 } };
        var hash = ConfigHasher.ComputeHash(data);

        _store.GetActiveVersionInfoAsync("test", Arg.Any<CancellationToken>())
            .Returns(new ActiveVersionInfo(hash, 5));

        var engine = CreateEngine();
        var result = await engine.ImportAsync("test", data);

        Assert.False(result.Changed);
        await _store.DidNotReceive().CreateVersionAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RollbackAsync_ActivatesPreviousVersion()
    {
        _store.GetVersionAsync("test", 1, Arg.Any<CancellationToken>())
            .Returns(new ConfigVersion("test", 1, "{}", "hash1", false, DateTimeOffset.UtcNow, null));
        _store.ActivateVersionAsync("test", 1, Arg.Any<CancellationToken>()).Returns(true);

        var engine = CreateEngine();
        var result = await engine.RollbackAsync("test", 1);

        Assert.True(result.Changed);
        await _store.Received().ActivateVersionAsync("test", 1, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoadAndApplyAsync_FromStore_Applies()
    {
        var version = new ConfigVersion("test", 1, "[1,2,3]", "hash", true, DateTimeOffset.UtcNow, null);
        _store.GetActiveAsync("test", Arg.Any<CancellationToken>()).Returns(version);

        var applier = Substitute.For<IConfigApplier>();
        applier.ConfigType.Returns("test");
        _applierRegistry.Register(applier);

        var engine = CreateEngine();
        await engine.LoadAndApplyAsync("test");

        await applier.Received().ApplyAsync("[1,2,3]", 1, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoadAndApplyAsync_FromDistributor_Applies()
    {
        var distributor = Substitute.For<IConfigDistributor>();
        distributor.GetAsync("test", Arg.Any<CancellationToken>())
            .Returns(new ConfigDistributorEntry("[4,5]", 2));

        var applier = Substitute.For<IConfigApplier>();
        applier.ConfigType.Returns("test");
        _applierRegistry.Register(applier);

        var engine = CreateEngine(distributor);
        await engine.LoadAndApplyAsync("test");

        await applier.Received().ApplyAsync("[4,5]", 2, Arg.Any<CancellationToken>());
        // Store should NOT be called when distributor has data
        await _store.DidNotReceive().GetActiveAsync("test", Arg.Any<CancellationToken>());
    }
}
