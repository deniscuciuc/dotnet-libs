using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace DenisCuciuc.Platform.Sentry;

public static class SentryExtensions
{
    /// <summary>
    /// Adds Sentry to the host using DenisCuciuc.Platform's opinionated defaults.
    /// Configuration is read from the <c>Sentry</c> section (or the section at
    /// <paramref name="configure"/>'s override) and then overridden by the delegate so
    /// callers can make targeted adjustments without replacing the full options object.
    /// Returns the builder unchanged (without enabling Sentry) when <c>Enabled</c> is
    /// <c>false</c> or no DSN is configured — safe for local dev and test environments.
    /// </summary>
    public static WebApplicationBuilder AddPlatformSentry(
        this WebApplicationBuilder builder,
        Action<PlatformSentryOptions>? configure = null)
    {
        var options = new PlatformSentryOptions();
        builder.Configuration.GetSection(PlatformSentryOptions.DefaultSectionPath).Bind(options);
        configure?.Invoke(options);

        if (!options.Enabled || string.IsNullOrWhiteSpace(options.Dsn))
            return builder;

        builder.WebHost.UseSentry(sentry =>
        {
            sentry.Dsn = options.Dsn;
            sentry.Environment = options.Environment;
            sentry.TracesSampleRate = options.TracesSampleRate;
            sentry.Debug = options.Debug;
            sentry.Release = options.Release;
            sentry.ServerName = options.ServerName ?? Environment.MachineName;
            sentry.AttachStacktrace = true;
        });

        return builder;
    }
}
