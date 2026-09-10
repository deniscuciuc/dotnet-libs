namespace CoreLibs.LiveConfig;

/// <summary>
/// Persistent storage for versioned configuration snapshots (e.g. MongoDB, Postgres).
/// </summary>
public interface IConfigStore
{
    /// <summary>
    /// Gets the currently active version for a config type.
    /// </summary>
    Task<ConfigVersion?> GetActiveAsync(string configType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific version.
    /// </summary>
    Task<ConfigVersion?> GetVersionAsync(string configType, int version, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the most recent N versions for a config type, ordered by version descending.
    /// </summary>
    Task<IReadOnlyList<ConfigVersion>> GetRecentVersionsAsync(string configType, int count = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest version number for a config type.
    /// </summary>
    Task<int> GetLatestVersionNumberAsync(string configType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the hash of the currently active version, or null if none exists.
    /// </summary>
    Task<string?> GetActiveHashAsync(string configType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the hash and version number of the currently active version in a single query.
    /// Returns null if no active version exists. More efficient than calling
    /// <see cref="GetActiveHashAsync"/> and <see cref="GetLatestVersionNumberAsync"/> separately.
    /// </summary>
    Task<ActiveVersionInfo?> GetActiveVersionInfoAsync(string configType,
        CancellationToken cancellationToken = default)
    {
        // Default implementation for backward compatibility — calls both methods separately.
        return GetActiveVersionInfoDefaultAsync(configType, cancellationToken);
    }

    private async Task<ActiveVersionInfo?> GetActiveVersionInfoDefaultAsync(string configType,
        CancellationToken cancellationToken)
    {
        var hash = await GetActiveHashAsync(configType, cancellationToken);
        if (hash is null) return null;
        var version = await GetLatestVersionNumberAsync(configType, cancellationToken);
        return new ActiveVersionInfo(hash, version);
    }

    /// <summary>
    /// Creates a new version. Returns the created version number.
    /// </summary>
    Task<int> CreateVersionAsync(string configType, string dataJson, string dataHash, string? createdBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates a specific version (deactivating the current active one atomically).
    /// Returns true if successful.
    /// </summary>
    Task<bool> ActivateVersionAsync(string configType, int version, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the manifest of all active config types and their versions.
    /// </summary>
    Task<IReadOnlyDictionary<string, int>> GetManifestAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes old versions for a config type according to the retention policy.
    /// The active version is never deleted when <see cref="ConfigRetentionOptions.KeepActiveAlways"/> is true.
    /// Returns the number of versions deleted.
    /// </summary>
    Task<int> CleanupOldVersionsAsync(string configType, ConfigRetentionOptions retention,
        CancellationToken cancellationToken = default);
}
