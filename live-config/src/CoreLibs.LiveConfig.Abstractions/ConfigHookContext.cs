namespace CoreLibs.LiveConfig;

/// <summary>
/// Context passed to lifecycle hooks before and after config application.
/// </summary>
/// <param name="ConfigType">The configuration type identifier.</param>
/// <param name="DataJson">The serialized JSON data.</param>
/// <param name="Version">The version being applied.</param>
/// <param name="IsRollback">Whether this is a rollback operation.</param>
public sealed record ConfigHookContext(
    string ConfigType,
    string DataJson,
    int Version,
    bool IsRollback);
