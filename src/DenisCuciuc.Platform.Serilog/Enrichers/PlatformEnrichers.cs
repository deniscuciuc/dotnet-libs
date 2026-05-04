using DenisCuciuc.Platform.Serilog.Configuration;
using Serilog;

namespace DenisCuciuc.Platform.Serilog.Enrichers;

internal static class PlatformEnrichers
{
    internal static void Apply(LoggerConfiguration lc, PlatformSerilogOptions options)
    {
        lc.Enrich.FromLogContext();

        if (options.EnableMachineName)
            lc.Enrich.WithMachineName();

        if (options.EnableEnvironment)
            lc.Enrich.WithEnvironmentName();

        if (options.EnableCorrelationId)
            lc.Enrich.WithCorrelationId();

        if (!string.IsNullOrWhiteSpace(options.ApplicationName))
            lc.Enrich.WithProperty("Application", options.ApplicationName);
    }
}
