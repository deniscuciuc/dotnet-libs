using CoreLibs.Serilog.Configuration;
using Serilog;

namespace CoreLibs.Serilog.Enrichers;

internal static class CoreEnrichers
{
    internal static void Apply(LoggerConfiguration lc, CoreSerilogOptions options)
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
