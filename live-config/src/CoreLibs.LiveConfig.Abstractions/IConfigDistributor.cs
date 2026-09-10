namespace CoreLibs.LiveConfig;

/// <summary>
/// Distributes configuration data for fast access and notifies consumers of updates (e.g. Redis).
/// </summary>
public interface IConfigDistributor
{
    /// <summary>
    /// Stores a config entry for distribution.
    /// </summary>
    Task SetAsync(string configType, string dataJson, int version, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the distributed config entry.
    /// </summary>
    Task<ConfigDistributorEntry?> GetAsync(string configType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a distributed config entry.
    /// </summary>
    Task DeleteAsync(string configType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes an update notification to all subscribers.
    /// </summary>
    Task PublishUpdateAsync(ConfigUpdateNotification notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes to config update notifications.
    /// </summary>
    Task SubscribeAsync(Func<ConfigUpdateNotification, Task> handler, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the manifest of all distributed config types and their versions.
    /// </summary>
    Task<IReadOnlyDictionary<string, int>> GetManifestAsync(CancellationToken cancellationToken = default);
}
