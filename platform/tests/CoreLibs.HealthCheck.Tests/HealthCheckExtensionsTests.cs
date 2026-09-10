using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CoreLibs.HealthCheck.Tests;

public class HealthCheckExtensionsTests
{
    [Fact]
    public void AddCoreHealthChecks_RegistersHealthCheckService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCoreHealthChecks();

        var provider = services.BuildServiceProvider();

        // HealthCheckService should be resolvable
        var svc = provider.GetRequiredService<HealthCheckService>();
        Assert.NotNull(svc);
    }

    [Fact]
    public async Task AddCoreHealthChecks_OkCheckPassesByDefault()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCoreHealthChecks();

        var provider = services.BuildServiceProvider();
        var healthCheckService = provider.GetRequiredService<HealthCheckService>();

        var report = await healthCheckService.CheckHealthAsync();

        Assert.Equal(HealthStatus.Healthy, report.Status);
    }

    [Fact]
    public void UseCoreHealthChecks_ConfiguresEndpoint()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddCoreHealthChecks();

        var app = builder.Build();
        // Should not throw when setting up health check endpoint
        var result = app.UseCoreHealthChecks("/health");
        Assert.NotNull(result);
    }

    [Fact]
    public void UseCoreHealthChecks_UsesCustomPath()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddCoreHealthChecks();

        var app = builder.Build();
        var result = app.UseCoreHealthChecks("/api/health");
        Assert.NotNull(result);
    }
}
