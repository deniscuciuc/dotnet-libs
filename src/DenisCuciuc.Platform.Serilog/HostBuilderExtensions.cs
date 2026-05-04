using DenisCuciuc.Platform.Serilog.Configuration;
using DenisCuciuc.Platform.Serilog.Enrichers;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace DenisCuciuc.Platform.Serilog;

public static class HostBuilderExtensions
{
    /// <summary>
    /// Configures Serilog as the logging provider for the host.
    /// Profile-level overrides are applied first so that <c>appsettings.json</c>
    /// can always override them at runtime.
    /// </summary>
    public static IHostBuilder UsePlatformSerilog(
        this IHostBuilder host,
        Action<PlatformSerilogOptions>? configure = null)
    {
        var options = new PlatformSerilogOptions();
        configure?.Invoke(options);

        return host.UseSerilog((ctx, lc) =>
        {
            ApplyProfile(lc, options);
            lc.ReadFrom.Configuration(ctx.Configuration);
            PlatformEnrichers.Apply(lc, options);
        });
    }

    private static void ApplyProfile(LoggerConfiguration lc, PlatformSerilogOptions options)
    {
        switch (options.Profile)
        {
            case SerilogProfile.WebApi:
                lc.MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning);
                break;

            case SerilogProfile.Worker:
                lc.MinimumLevel.Override("Microsoft.Extensions.Hosting", LogEventLevel.Warning);
                break;

            case SerilogProfile.Minimal:
                lc.MinimumLevel.Override("Microsoft", LogEventLevel.Error);
                lc.MinimumLevel.Override("System", LogEventLevel.Error);
                break;

            case SerilogProfile.Debug:
                lc.MinimumLevel.Debug();
                break;
        }
    }
}
