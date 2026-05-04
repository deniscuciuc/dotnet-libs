using System.ComponentModel.DataAnnotations;

namespace DenisCuciuc.Platform.Jobs;

/// <summary>
/// Configuration options for the DenisCuciuc.Platform job scheduling system.
/// Bound from the <c>Jobs</c> configuration section.
/// </summary>
public sealed class PlatformJobsOptions
{
    public const string DefaultSectionPath = "Jobs";

    /// <summary>The job scheduling provider to use.</summary>
    [Required]
    public JobProviderKind Provider { get; set; } = JobProviderKind.Hangfire;

    /// <summary>The persistent job store to use.</summary>
    [Required]
    public JobStoreKind Store { get; set; } = JobStoreKind.Postgres;

    /// <summary>Maximum retry attempts for failed jobs.</summary>
    [Range(0, 10)]
    public int MaxRetries { get; set; } = 3;

    /// <summary>Default execution timeout in seconds.</summary>
    [Range(1, 3600)]
    public int DefaultTimeoutSeconds { get; set; } = 60;

    /// <summary>Dashboard configuration (Hangfire only).</summary>
    public PlatformJobsDashboardOptions Dashboard { get; set; } = new();
}

/// <summary>
/// Dashboard configuration options.
/// </summary>
public sealed class PlatformJobsDashboardOptions
{
    /// <summary>Whether the dashboard is enabled.</summary>
    public bool Enabled { get; set; }

    /// <summary>URL path for the dashboard.</summary>
    public string Path { get; set; } = "/jobs";

    /// <summary>Authentication mode for the dashboard.</summary>
    public DashboardAuthMode AuthMode { get; set; } = DashboardAuthMode.Identity;
}

public enum DashboardAuthMode
{
    /// <summary>Use DenisCuciuc.Platform.Identity claims-based auth.</summary>
    Identity,

    /// <summary>Allow only requests from localhost.</summary>
    AllowLocal,

    /// <summary>Allow all requests (development only).</summary>
    AllowAll
}
