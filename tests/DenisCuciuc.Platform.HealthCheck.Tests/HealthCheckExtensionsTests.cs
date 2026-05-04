using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DenisCuciuc.Platform.HealthCheck.Tests;

public class HealthCheckExtensionsTests
{
    [Fact]
    public void AddPlatformHealthChecks_RegistersHealthCheckService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPlatformHealthChecks();

        var provider = services.BuildServiceProvider();

        // HealthCheckService should be resolvable
        var svc = provider.GetRequiredService<HealthCheckService>();
        Assert.NotNull(svc);
    }

    [Fact]
    public async Task AddPlatformHealthChecks_OkCheckPassesByDefault()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPlatformHealthChecks();

        var provider = services.BuildServiceProvider();
        var healthCheckService = provider.GetRequiredService<HealthCheckService>();

        var report = await healthCheckService.CheckHealthAsync();

        Assert.Equal(HealthStatus.Healthy, report.Status);
    }

    [Fact]
    public void UsePlatformHealthChecks_ConfiguresEndpoint()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddPlatformHealthChecks();

        var app = builder.Build();
        // Should not throw when setting up health check endpoint
        var result = app.UsePlatformHealthChecks("/health");
        Assert.NotNull(result);
    }

    [Fact]
    public void UsePlatformHealthChecks_UsesCustomPath()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddPlatformHealthChecks();

        var app = builder.Build();
        var result = app.UsePlatformHealthChecks("/api/health");
        Assert.NotNull(result);
    }
}
