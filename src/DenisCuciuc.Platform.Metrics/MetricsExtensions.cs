using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;

namespace DenisCuciuc.Platform.Metrics;

public static class MetricsExtensions
{
    public static IServiceCollection AddPlatformMetrics(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionPath = PlatformMetricsOptions.DefaultSectionPath)
    {
        services
            .AddOptions<PlatformMetricsOptions>()
            .Bind(configuration.GetSection(sectionPath))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var options = configuration.GetSection(sectionPath).Get<PlatformMetricsOptions>()
                      ?? new PlatformMetricsOptions();

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

    public static IEndpointRouteBuilder MapPlatformMetrics(
        this IEndpointRouteBuilder endpoints,
        string? endpointPath = null)
    {
        var options = endpoints.ServiceProvider
                          .GetService<IOptionsMonitor<PlatformMetricsOptions>>()?
                          .CurrentValue
                      ?? new PlatformMetricsOptions();

        if (!options.Enabled)
            return endpoints;

        endpoints.MapPrometheusScrapingEndpoint(endpointPath ?? options.EndpointPath);

        return endpoints;
    }
}
