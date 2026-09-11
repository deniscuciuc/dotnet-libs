namespace CoreLibs.LiveConfig;

/// <summary>
/// Lightweight projection of the active version's hash and version number.
/// Used for efficient no-op detection without loading the full config data.
/// </summary>
public sealed record ActiveVersionInfo(string DataHash, int Version);
