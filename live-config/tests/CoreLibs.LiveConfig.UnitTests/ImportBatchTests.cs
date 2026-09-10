using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace CoreLibs.LiveConfig.UnitTests;

public class ImportBatchTests
{
    private readonly IConfigStore _store = Substitute.For<IConfigStore>();
    private readonly IConfigDistributor _distributor = Substitute.For<IConfigDistributor>();
    private readonly ConfigApplierRegistry _applierRegistry = new();
    private readonly ConfigHookRegistry _hookRegistry = new();
    private readonly NullLogger<LiveConfigEngine> _logger = NullLogger<LiveConfigEngine>.Instance;

    private LiveConfigEngine CreateEngine(bool withDistributor = true)
    {
        return new LiveConfigEngine(
            _store, _applierRegistry, _hookRegistry, _logger,
            withDistributor ? _distributor : null);
    }

    private static ConfigSnapshot Snap(string configType, string json)
    {
        return new ConfigSnapshot(configType, json, ConfigHasher.ComputeHash(json), DateTimeOffset.UtcNow);
    }

    // ═══════════════════════════════════════════════════════════
    // Partial Mode
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task Partial_EmptyBatch_ReturnsEmptyResult()
    {
        var engine = CreateEngine();
        var result = await engine.ImportBatchAsync(
            new Dictionary<string, ConfigSnapshot>(), ImportTransactionMode.Partial);

        Assert.Empty(result.Results);
        Assert.Equal(ImportTransactionMode.Partial, result.Mode);
        Assert.False(result.WasRolledBack);
    }

    [Fact]
    public async Task Partial_AllNew_ImportsEach()
    {
        _store.GetActiveHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((string?)null);
        _store.CreateVersionAsync("alpha", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(),
            Arg.Any<CancellationToken>()).Returns(1);
        _store.CreateVersionAsync("beta", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(),
            Arg.Any<CancellationToken>()).Returns(1);
        _store.ActivateVersionAsync(Arg.Any<string>(), 1, Arg.Any<CancellationToken>()).Returns(true);

        var snapshots = new Dictionary<string, ConfigSnapshot>
        {
            ["alpha"] = Snap("alpha", "[1]"),
            ["beta"] = Snap("beta", "[2]")
        };

        var engine = CreateEngine();
        var result = await engine.ImportBatchAsync(snapshots, ImportTransactionMode.Partial, "test");

        Assert.Equal(2, result.Results.Count);
        Assert.True(result.AllSucceeded);
        Assert.Equal(2, result.ChangedCount);
        Assert.Equal(ImportTransactionMode.Partial, result.Mode);
        Assert.False(result.WasRolledBack);
    }

    [Fact]
    public async Task Partial_OneFailsOtherSucceeds()
    {
        // "alpha" succeeds
        _store.GetActiveHashAsync("alpha", Arg.Any<CancellationToken>()).Returns((string?)null);
        _store.CreateVersionAsync("alpha", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(),
            Arg.Any<CancellationToken>()).Returns(1);
        _store.ActivateVersionAsync("alpha", 1, Arg.Any<CancellationToken>()).Returns(true);

        // "beta" throws during CreateVersion
        _store.GetActiveHashAsync("beta", Arg.Any<CancellationToken>()).Returns((string?)null);
        _store.CreateVersionAsync("beta", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Store unavailable"));

        var snapshots = new Dictionary<string, ConfigSnapshot>
        {
            ["alpha"] = Snap("alpha", "[1]"),
            ["beta"] = Snap("beta", "[2]")
        };

        var engine = CreateEngine();
        var result = await engine.ImportBatchAsync(snapshots, ImportTransactionMode.Partial, "test");

        Assert.Equal(2, result.Results.Count);
        Assert.False(result.AllSucceeded);
        Assert.True(result.HasErrors);
        Assert.Equal(1, result.ChangedCount);
        Assert.Equal(1, result.FailedCount);
        Assert.False(result.WasRolledBack);

        // Alpha succeeded
        var alpha = result.Results.Single(r => r.ConfigType == "alpha");
        Assert.True(alpha.IsSuccess);
        Assert.True(alpha.Changed);

        // Beta failed
        var beta = result.Results.Single(r => r.ConfigType == "beta");
        Assert.False(beta.IsSuccess);
        Assert.Contains("Store unavailable", beta.Error);
    }

    [Fact]
    public async Task Partial_HashMatch_SkipsUnchanged()
    {
        var json = "[1,2,3]";
        var hash = ConfigHasher.ComputeHash(json);

        _store.GetActiveVersionInfoAsync("unchanged", Arg.Any<CancellationToken>())
            .Returns(new ActiveVersionInfo(hash, 5));

        var snapshots = new Dictionary<string, ConfigSnapshot>
        {
            ["unchanged"] = Snap("unchanged", json)
        };

        var engine = CreateEngine();
        var result = await engine.ImportBatchAsync(snapshots, ImportTransactionMode.Partial, "test");

        Assert.Single(result.Results);
        Assert.True(result.AllSucceeded);
        Assert.Equal(0, result.ChangedCount);
        Assert.Equal(1, result.UnchangedCount);

        await _store.DidNotReceive().CreateVersionAsync(
            "unchanged", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    // ═══════════════════════════════════════════════════════════
    // Transactional Mode
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task Transactional_EmptyBatch_ReturnsEmptyResult()
    {
        var engine = CreateEngine();
        var result = await engine.ImportBatchAsync(
            new Dictionary<string, ConfigSnapshot>(), ImportTransactionMode.Transactional);

        Assert.Empty(result.Results);
        Assert.Equal(ImportTransactionMode.Transactional, result.Mode);
    }

    [Fact]
    public async Task Transactional_AllSucceed_ActivatesAll()
    {
        _store.GetActiveHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((string?)null);
        _store.CreateVersionAsync("items", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(),
            Arg.Any<CancellationToken>()).Returns(1);
        _store.CreateVersionAsync("prices", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(),
            Arg.Any<CancellationToken>()).Returns(1);
        _store.ActivateVersionAsync(Arg.Any<string>(), 1, Arg.Any<CancellationToken>()).Returns(true);

        var snapshots = new Dictionary<string, ConfigSnapshot>
        {
            ["items"] = Snap("items", "[{\"id\":1}]"),
            ["prices"] = Snap("prices", "[{\"price\":10}]")
        };

        var engine = CreateEngine();
        var result = await engine.ImportBatchAsync(snapshots, ImportTransactionMode.Transactional, "test");

        Assert.Equal(2, result.Results.Count);
        Assert.True(result.AllSucceeded);
        Assert.Equal(2, result.ChangedCount);
        Assert.False(result.WasRolledBack);
        Assert.Equal(ImportTransactionMode.Transactional, result.Mode);

        // Both should be activated
        await _store.Received().ActivateVersionAsync("items", 1, Arg.Any<CancellationToken>());
        await _store.Received().ActivateVersionAsync("prices", 1, Arg.Any<CancellationToken>());

        // Both should be distributed
        await _distributor.Received().SetAsync("items", Arg.Any<string>(), 1, Arg.Any<CancellationToken>());
        await _distributor.Received().SetAsync("prices", Arg.Any<string>(), 1, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Transactional_StagingFails_NothingActivated()
    {
        // "items" stages OK
        _store.GetActiveHashAsync("items", Arg.Any<CancellationToken>()).Returns((string?)null);
        _store.CreateVersionAsync("items", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(),
            Arg.Any<CancellationToken>()).Returns(1);

        // "prices" throws during staging
        _store.GetActiveHashAsync("prices", Arg.Any<CancellationToken>()).Returns((string?)null);
        _store.CreateVersionAsync("prices", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Disk full"));

        var snapshots = new Dictionary<string, ConfigSnapshot>
        {
            ["items"] = Snap("items", "[{\"id\":1}]"),
            ["prices"] = Snap("prices", "[{\"price\":10}]")
        };

        var engine = CreateEngine();
        var result = await engine.ImportBatchAsync(snapshots, ImportTransactionMode.Transactional, "test");

        Assert.True(result.WasRolledBack);
        Assert.True(result.HasErrors);
        Assert.Equal(ImportTransactionMode.Transactional, result.Mode);

        // Nothing should be activated
        await _store.DidNotReceive()
            .ActivateVersionAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>());

        // Nothing should be distributed
        await _distributor.DidNotReceive().SetAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Transactional_MixedHashMatch_SkipsUnchangedStillCommits()
    {
        var unchangedJson = "[1,2,3]";
        var unchangedHash = ConfigHasher.ComputeHash(unchangedJson);

        // "items" unchanged
        _store.GetActiveVersionInfoAsync("items", Arg.Any<CancellationToken>())
            .Returns(new ActiveVersionInfo(unchangedHash, 3));

        // "prices" changed
        _store.GetActiveHashAsync("prices", Arg.Any<CancellationToken>()).Returns((string?)null);
        _store.CreateVersionAsync("prices", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(),
            Arg.Any<CancellationToken>()).Returns(4);
        _store.ActivateVersionAsync("prices", 4, Arg.Any<CancellationToken>()).Returns(true);

        var snapshots = new Dictionary<string, ConfigSnapshot>
        {
            ["items"] = Snap("items", unchangedJson),
            ["prices"] = Snap("prices", "[{\"price\":99}]")
        };

        var engine = CreateEngine();
        var result = await engine.ImportBatchAsync(snapshots, ImportTransactionMode.Transactional, "test");

        Assert.True(result.AllSucceeded);
        Assert.False(result.WasRolledBack);
        Assert.Equal(1, result.ChangedCount);
        Assert.Equal(1, result.UnchangedCount);

        // Only prices should be activated
        await _store.DidNotReceive().ActivateVersionAsync("items", Arg.Any<int>(), Arg.Any<CancellationToken>());
        await _store.Received(1).ActivateVersionAsync("prices", 4, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Transactional_WithoutDistributor_StillWorks()
    {
        _store.GetActiveHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((string?)null);
        _store.CreateVersionAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(),
            Arg.Any<CancellationToken>()).Returns(1);
        _store.ActivateVersionAsync(Arg.Any<string>(), 1, Arg.Any<CancellationToken>()).Returns(true);

        var snapshots = new Dictionary<string, ConfigSnapshot>
        {
            ["single"] = Snap("single", "[42]")
        };

        var engine = CreateEngine(false);
        var result = await engine.ImportBatchAsync(snapshots, ImportTransactionMode.Transactional, "test");

        Assert.True(result.AllSucceeded);
        Assert.Equal(1, result.ChangedCount);
    }

    [Fact]
    public async Task DefaultMode_IsPartial()
    {
        var engine = CreateEngine();
        var result = await engine.ImportBatchAsync(new Dictionary<string, ConfigSnapshot>());

        Assert.Equal(ImportTransactionMode.Partial, result.Mode);
    }
}
