using CoreLibs.Startup;

namespace CoreLibs.Localization.Store.Postgres;

/// <summary>
/// Ensures the localization_entries table exists before other startups run.
/// </summary>
public sealed class PostgresLocalizationStoreStartup(PostgresLocalizationStore store) : IStartup
{
    public int Order => -1;

    public Task StartupAsync(IHostStartup host, CancellationToken cancellationToken = default) =>
        store.EnsureSchemaAsync(cancellationToken);
}
