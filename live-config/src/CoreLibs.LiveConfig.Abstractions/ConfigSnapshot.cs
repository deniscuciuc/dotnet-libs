namespace CoreLibs.LiveConfig;

/// <summary>
/// Represents a point-in-time snapshot of configuration data fetched from a source.
/// </summary>
/// <param name="ConfigType">The configuration type identifier.</param>
/// <param name="DataJson">The serialized JSON data.</param>
/// <param name="DataHash">SHA-256 hash of the data for no-op detection.</param>
/// <param name="FetchedAt">When the snapshot was fetched.</param>
public sealed record ConfigSnapshot(
    string ConfigType,
    string DataJson,
    string DataHash,
    DateTimeOffset FetchedAt);
