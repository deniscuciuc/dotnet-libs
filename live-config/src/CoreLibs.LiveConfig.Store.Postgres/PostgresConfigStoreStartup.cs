using CoreLibs.Startup;

namespace CoreLibs.LiveConfig.Store.Postgres;

/// <summary>
/// Ensures the config_snapshots table exists before other startups run.
/// </summary>
public sealed class PostgresConfigStoreStartup(PostgresConfigStore store) : IStartup
{
    public int Order => -1;

    public Task StartupAsync(IHostStartup host, CancellationToken cancellationToken = default) =>
        store.EnsureSchemaAsync(cancellationToken);
}
