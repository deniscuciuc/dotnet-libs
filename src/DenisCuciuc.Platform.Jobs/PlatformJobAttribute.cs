namespace DenisCuciuc.Platform.Jobs;

/// <summary>
/// Optional per-job configuration. Values of <c>-1</c> fall back to <see cref="PlatformJobsOptions"/> defaults.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class PlatformJobAttribute : Attribute
{
    /// <summary>Maximum retry attempts. <c>-1</c> = use global default.</summary>
    public int Retries { get; set; } = -1;

    /// <summary>Execution timeout in seconds. <c>-1</c> = use global default.</summary>
    public int TimeoutSeconds { get; set; } = -1;

    /// <summary>Named queue for routing. <c>null</c> = default queue.</summary>
    public string? Queue { get; set; }
}
