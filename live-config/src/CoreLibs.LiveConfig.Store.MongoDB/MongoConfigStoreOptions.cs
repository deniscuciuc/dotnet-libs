namespace CoreLibs.LiveConfig.Store.MongoDB;

/// <summary>
/// Options for the MongoDB config store.
/// </summary>
public sealed class MongoConfigStoreOptions
{
    public const string SectionPath = "LiveConfig:Store:MongoDB";

    /// <summary>
    /// MongoDB collection name for config snapshots.
    /// </summary>
    public string CollectionName { get; set; } = "config_snapshots";
}
