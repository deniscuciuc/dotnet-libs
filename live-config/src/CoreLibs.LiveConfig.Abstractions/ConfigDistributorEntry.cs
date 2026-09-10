namespace CoreLibs.LiveConfig;

/// <summary>
/// An entry stored in the distributor (e.g. Redis) for fast access.
/// </summary>
/// <param name="DataJson">The serialized JSON data.</param>
/// <param name="Version">The version number.</param>
public sealed record ConfigDistributorEntry(
    string DataJson,
    int Version);
