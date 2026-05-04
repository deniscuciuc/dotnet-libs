namespace DenisCuciuc.Platform.Sentry;

public sealed class PlatformSentryOptions
{
    public const string DefaultSectionPath = "Sentry";

    public bool Enabled { get; set; } = true;

    public string? Dsn { get; set; }

    public string Environment { get; set; } = "development";

    /// <summary>
    /// Fraction of transactions captured for performance monitoring. Range 0.0–1.0.
    /// Defaults to <c>0.1</c> (10 %) to avoid overhead in production.
    /// </summary>
    public double TracesSampleRate { get; set; } = 0.1;

    public bool Debug { get; set; } = false;

    /// <summary>Release identifier sent with every event, e.g. <c>"my-api@1.2.3"</c>.</summary>
    public string? Release { get; set; }

    /// <summary>Overrides the server name tag. Defaults to <see cref="Environment.MachineName"/>.</summary>
    public string? ServerName { get; set; }
}
