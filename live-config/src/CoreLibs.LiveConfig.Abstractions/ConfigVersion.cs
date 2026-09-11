namespace CoreLibs.LiveConfig;

/// <summary>
/// Represents a versioned configuration entry persisted in a store.
/// </summary>
/// <param name="ConfigType">The configuration type identifier.</param>
/// <param name="Version">The sequential version number.</param>
/// <param name="DataJson">The serialized JSON data.</param>
/// <param name="DataHash">SHA-256 hash of the data.</param>
/// <param name="IsActive">Whether this version is the currently active one.</param>
/// <param name="CreatedAt">When this version was created.</param>
/// <param name="CreatedBy">Who or what created this version.</param>
public sealed record ConfigVersion(
    string ConfigType,
    int Version,
    string DataJson,
    string DataHash,
    bool IsActive,
    DateTimeOffset CreatedAt,
    string? CreatedBy);
