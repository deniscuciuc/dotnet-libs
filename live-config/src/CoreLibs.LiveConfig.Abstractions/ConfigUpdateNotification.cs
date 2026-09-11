namespace CoreLibs.LiveConfig;

/// <summary>
/// Notification published when a configuration is updated or rolled back.
/// </summary>
/// <param name="ConfigType">The configuration type identifier.</param>
/// <param name="Version">The new active version.</param>
/// <param name="IsRollback">Whether this was a rollback operation.</param>
public sealed record ConfigUpdateNotification(
    string ConfigType,
    int Version,
    bool IsRollback);
