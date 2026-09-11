using CoreLibs.LiveConfig.Store.Postgres;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Npgsql;
using Testcontainers.PostgreSql;

namespace CoreLibs.LiveConfig.IntegrationTests;

public class PostgresConfigStoreTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:15.1").Build();
    private PostgresConfigStore _store = null!;
    private NpgsqlDataSource _dataSource = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        _dataSource = NpgsqlDataSource.Create(_container.GetConnectionString());

        _store = new PostgresConfigStore(
            _dataSource,
            Options.Create(new PostgresConfigStoreOptions()),
            NullLogger<PostgresConfigStore>.Instance);

        await _store.EnsureSchemaAsync();
    }

    public async Task DisposeAsync()
    {
        await _dataSource.DisposeAsync();
        await _container.DisposeAsync();
    }

    [Fact]
    public async Task CreateVersion_And_GetActive_RoundTrip()
    {
        var version = await _store.CreateVersionAsync("pg_test", """{"key":"value"}""", "hash1");
        Assert.Equal(1, version);

        var activated = await _store.ActivateVersionAsync("pg_test", version);
        Assert.True(activated);

        var active = await _store.GetActiveAsync("pg_test");
        Assert.NotNull(active);
        Assert.Equal(1, active.Version);
        Assert.True(active.IsActive);
    }

    [Fact]
    public async Task GetRecentVersions_ReturnsDescending()
    {
        await _store.CreateVersionAsync("pg_ordered", "{}", "h1");
        await _store.CreateVersionAsync("pg_ordered", "{}", "h2");
        await _store.CreateVersionAsync("pg_ordered", "{}", "h3");

        var versions = await _store.GetRecentVersionsAsync("pg_ordered", 10);
        Assert.Equal(3, versions.Count);
        Assert.Equal(3, versions[0].Version);
    }

    [Fact]
    public async Task GetManifest_ReturnsActiveVersions()
    {
        await _store.CreateVersionAsync("pg_alpha", "{}", "ha");
        await _store.ActivateVersionAsync("pg_alpha", 1);
        await _store.CreateVersionAsync("pg_beta", "{}", "hb");
        await _store.ActivateVersionAsync("pg_beta", 1);

        var manifest = await _store.GetManifestAsync();
        Assert.Contains("pg_alpha", manifest.Keys);
        Assert.Contains("pg_beta", manifest.Keys);
    }
}
