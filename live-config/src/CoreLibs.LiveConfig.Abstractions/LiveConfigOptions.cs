namespace CoreLibs.LiveConfig;

/// <summary>
/// Core options for the LiveConfig framework.
/// </summary>
public sealed class LiveConfigOptions
{
    /// <summary>
    /// Configuration section path. Default: "LiveConfig".
    /// </summary>
    public const string SectionPath = "LiveConfig";

    /// <summary>
    /// Current environment name (e.g. Development, Staging, Production).
    /// Auto-detected from IHostEnvironment if not set.
    /// </summary>
    public string? Environment { get; set; }

    /// <summary>
    /// Config types that must be loaded before the application starts accepting traffic.
    /// </summary>
    public HashSet<string> CriticalConfigTypes { get; set; } = [];

    /// <summary>
    /// When true (default), critical config types are preloaded automatically on startup.
    /// When false, configs are only loaded on explicit demand (API/MQ request).
    /// </summary>
    public bool EnablePreload { get; set; } = true;

    /// <summary>
    /// Optional source name to use when startup preload must bootstrap a missing config from a source.
    /// When omitted, startup bootstrap uses the only registered source, if exactly one exists.
    /// </summary>
    public string? PreloadSourceName { get; set; }

    /// <summary>
    /// When true, always fetches configs from the configured source on startup, even if the store
    /// already has data. Ensures the application always starts with the latest source data.
    /// Default: false (use store data when available, only bootstrap from source when missing).
    /// </summary>
    public bool ForceSourceRefresh { get; set; }

    /// <summary>
    /// Interval for polling fallback when distributor pub/sub is unavailable.
    /// </summary>
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Enable polling fallback for environments without pub/sub.
    /// </summary>
    public bool EnablePollingFallback { get; set; }

    /// <summary>
    /// Default transaction mode for batch imports when not specified per request.
    /// Overridable per-request via endpoint query parameter or explicit API call.
    /// </summary>
    public ImportTransactionMode DefaultImportMode { get; set; } = ImportTransactionMode.Partial;

    /// <summary>
    /// Retry options for transient failures.
    /// </summary>
    public RetryOptions Retry { get; set; } = new();
}

/// <summary>
/// Retry configuration for transient failures.
/// </summary>
public sealed class RetryOptions
{
    /// <summary>
    /// Maximum number of retries.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Initial delay before the first retry.
    /// </summary>
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromMilliseconds(250);

    /// <summary>
    /// Maximum delay between retries.
    /// </summary>
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(5);
}
