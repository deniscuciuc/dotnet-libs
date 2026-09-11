namespace CoreLibs.LiveConfig.Store.Postgres;

/// <summary>
/// Options for the Postgres config store.
/// </summary>
public sealed class PostgresConfigStoreOptions
{
    public const string SectionPath = "LiveConfig:Store:Postgres";

    /// <summary>
    /// Schema name for the config snapshots table.
    /// </summary>
    public string Schema { get; set; } = "public";

    /// <summary>
    /// Table name for config snapshots.
    /// </summary>
    public string TableName { get; set; } = "config_snapshots";
}
