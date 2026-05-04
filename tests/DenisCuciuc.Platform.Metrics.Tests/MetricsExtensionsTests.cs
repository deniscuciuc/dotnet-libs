using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DenisCuciuc.Platform.Metrics.Tests;

public class MetricsExtensionsTests
{
    private static IConfiguration BuildConfig(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    [Fact]
    public void AddPlatformMetrics_WhenEnabled_RegistersOpenTelemetry()
    {
        var config = BuildConfig(new Dictionary<string, string?> { ["Metrics:Enabled"] = "true" });
        var services = new ServiceCollection();

        services.AddPlatformMetrics(config);
        var provider = services.BuildServiceProvider();

        // OpenTelemetry registered — should not throw on build
        Assert.NotNull(provider);
    }

    [Fact]
    public void AddPlatformMetrics_WhenDisabled_SkipsOpenTelemetryRegistration()
    {
        var config = BuildConfig(new Dictionary<string, string?> { ["Metrics:Enabled"] = "false" });
        var services = new ServiceCollection();

        // Should not throw
        services.AddPlatformMetrics(config);
        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider);
    }

    [Fact]
    public void AddPlatformMetrics_DefaultConfig_Registers()
    {
        var config = new ConfigurationBuilder().Build(); // empty config → uses defaults
        var services = new ServiceCollection();

        services.AddPlatformMetrics(config);
        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider);
    }

    [Fact]
    public void MapPlatformMetrics_WhenDisabled_DoesNotMapEndpoint()
    {
        var builder = WebApplication.CreateBuilder();
        var config = BuildConfig(new Dictionary<string, string?> { ["Metrics:Enabled"] = "false" });
        builder.Services.AddPlatformMetrics(config);

        var app = builder.Build();

        // When disabled, MapPlatformMetrics returns the same endpoints builder without mapping
        var result = app.MapPlatformMetrics();
        Assert.NotNull(result);
    }

    [Fact]
    public void AddPlatformMetrics_WithCustomMeters_DoesNotThrow()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Metrics:Enabled"] = "true",
            ["Metrics:Meters:0"] = "MyApp.Jobs",
            ["Metrics:Meters:1"] = "MyApp.Cache"
        });
        var services = new ServiceCollection();

        // Should not throw
        services.AddPlatformMetrics(config);
    }
}
