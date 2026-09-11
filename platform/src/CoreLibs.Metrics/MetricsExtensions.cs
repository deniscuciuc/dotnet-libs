using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;

namespace CoreLibs.Metrics;

public static class MetricsExtensions
{
    public static IServiceCollection AddCoreMetrics(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionPath = CoreMetricsOptions.DefaultSectionPath)
    {
        services
            .AddOptions<CoreMetricsOptions>()
            .Bind(configuration.GetSection(sectionPath))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var options = configuration.GetSection(sectionPath).Get<CoreMetricsOptions>()
                      ?? new CoreMetricsOptions();

        if (!options.Enabled)
            return services;

        services.AddOpenTelemetry()
            .WithMetrics(builder =>
            {
                if (options.EnableAspNetCoreInstrumentation)
                {
                    builder.AddAspNetCoreInstrumentation();
                }

                if (options.Meters.Length > 0)
                {
                    builder.AddMeter(options.Meters);
                }

                builder.AddPrometheusExporter();
            });

        return services;
    }

    public static IEndpointRouteBuilder MapCoreMetrics(
        this IEndpointRouteBuilder endpoints,
        string? endpointPath = null)
    {
        var options = endpoints.ServiceProvider
                          .GetService<IOptionsMonitor<CoreMetricsOptions>>()?
                          .CurrentValue
                      ?? new CoreMetricsOptions();

        if (!options.Enabled)
            return endpoints;

        endpoints.MapPrometheusScrapingEndpoint(endpointPath ?? options.EndpointPath);

        return endpoints;
    }
}
