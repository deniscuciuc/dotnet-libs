using System.ComponentModel.DataAnnotations;

namespace DenisCuciuc.Platform.Metrics;

public sealed class PlatformMetricsOptions
{
    public const string DefaultSectionPath = "Metrics";

    public bool Enabled { get; set; } = true;

    public bool EnableAspNetCoreInstrumentation { get; set; } = true;

    [RegularExpression("^/.*", ErrorMessage = "EndpointPath must start with '/'.")]
    public string EndpointPath { get; set; } = "/metrics";

    public string[] Meters { get; set; } = Array.Empty<string>();
}
