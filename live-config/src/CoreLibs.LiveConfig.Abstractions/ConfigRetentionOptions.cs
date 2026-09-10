namespace CoreLibs.LiveConfig;

/// <summary>
/// Retention policy for versioned config snapshots in the store.
/// Applied after each new version is created and activated.
/// </summary>
public sealed class ConfigRetentionOptions
{
    /// <summary>
    /// Maximum number of versions to retain per config type.
    /// The active version is always retained regardless of this limit.
    /// Set to 0 to disable count-based cleanup. Default: 30.
    /// </summary>
    public int MaxVersions { get; set; } = 30;

    /// <summary>
    /// Optional maximum age for retained versions.
    /// Versions older than this are eligible for deletion (unless active).
    /// When set together with <see cref="MaxVersions"/>, a version is retained
    /// if it satisfies <b>either</b> criterion (within top N <b>or</b> younger than MaxAge).
    /// Set to <c>null</c> to disable age-based cleanup.
    /// </summary>
    public TimeSpan? MaxAge { get; set; }

    /// <summary>
    /// When true, the currently active version is never deleted regardless of
    /// count or age limits. Default: true.
    /// </summary>
    public bool KeepActiveAlways { get; set; } = true;

    /// <summary>
    /// Whether retention cleanup is enabled. Default: true.
    /// </summary>
    public bool Enabled { get; set; } = true;
}
