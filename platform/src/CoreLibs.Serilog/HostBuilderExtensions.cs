using CoreLibs.Serilog.Configuration;
using CoreLibs.Serilog.Enrichers;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace CoreLibs.Serilog;

public static class HostBuilderExtensions
{
    /// <summary>
    /// Configures Serilog as the logging provider for the host.
    /// Profile-level overrides are applied first so that <c>appsettings.json</c>
    /// can always override them at runtime.
    /// </summary>
    public static IHostBuilder UseCoreSerilog(
        this IHostBuilder host,
        Action<CoreSerilogOptions>? configure = null)
    {
        var options = new CoreSerilogOptions();
        configure?.Invoke(options);

        return host.UseSerilog((ctx, lc) =>
        {
            ApplyProfile(lc, options);
            lc.ReadFrom.Configuration(ctx.Configuration);
            CoreEnrichers.Apply(lc, options);
        });
    }

    /// <summary>
    /// Configures Serilog for <see cref="IHostApplicationBuilder"/> hosts
    /// (e.g. <c>Host.CreateApplicationBuilder()</c>).
    /// </summary>
    public static IHostApplicationBuilder UseCoreSerilog(
        this IHostApplicationBuilder builder,
        Action<CoreSerilogOptions>? configure = null)
    {
        var options = new CoreSerilogOptions();
        configure?.Invoke(options);

        builder.Services.AddSerilog((_, lc) =>
        {
            ApplyProfile(lc, options);
            lc.ReadFrom.Configuration(builder.Configuration);
            CoreEnrichers.Apply(lc, options);
        });

        return builder;
    }

    private static void ApplyProfile(LoggerConfiguration lc, CoreSerilogOptions options)
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
