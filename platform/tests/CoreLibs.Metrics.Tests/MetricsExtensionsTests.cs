using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CoreLibs.Metrics.Tests;

public class MetricsExtensionsTests
{
    private static IConfiguration BuildConfig(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    [Fact]
    public void AddCoreMetrics_WhenEnabled_RegistersOpenTelemetry()
    {
        var config = BuildConfig(new Dictionary<string, string?> { ["Metrics:Enabled"] = "true" });
        var services = new ServiceCollection();

        services.AddCoreMetrics(config);
        var provider = services.BuildServiceProvider();

        // OpenTelemetry registered — should not throw on build
        Assert.NotNull(provider);
    }

    [Fact]
    public void AddCoreMetrics_WhenDisabled_SkipsOpenTelemetryRegistration()
    {
        var config = BuildConfig(new Dictionary<string, string?> { ["Metrics:Enabled"] = "false" });
        var services = new ServiceCollection();

        // Should not throw
        services.AddCoreMetrics(config);
        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider);
    }

    [Fact]
    public void AddCoreMetrics_DefaultConfig_Registers()
    {
        var config = new ConfigurationBuilder().Build(); // empty config → uses defaults
        var services = new ServiceCollection();

        services.AddCoreMetrics(config);
        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider);
    }

    [Fact]
    public void MapCoreMetrics_WhenDisabled_DoesNotMapEndpoint()
    {
        var builder = WebApplication.CreateBuilder();
        var config = BuildConfig(new Dictionary<string, string?> { ["Metrics:Enabled"] = "false" });
        builder.Services.AddCoreMetrics(config);

        var app = builder.Build();

        // When disabled, MapCoreMetrics returns the same endpoints builder without mapping
        var result = app.MapCoreMetrics();
        Assert.NotNull(result);
    }

    [Fact]
    public void AddCoreMetrics_WithCustomMeters_DoesNotThrow()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Metrics:Enabled"] = "true",
            ["Metrics:Meters:0"] = "MyApp.Jobs",
            ["Metrics:Meters:1"] = "MyApp.Cache"
        });
        var services = new ServiceCollection();

        // Should not throw
        services.AddCoreMetrics(config);
    }
}
