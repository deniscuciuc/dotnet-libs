using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CoreLibs.HealthCheck.Tests;

public class OkHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_AlwaysReturnsHealthy()
    {
        var check = new OkHealthCheck();
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("ok", check, failureStatus: null, tags: [])
        };

        var result = await check.CheckHealthAsync(context);

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal("ok", result.Description);
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsHealthy_EvenWhenCalledConcurrently()
    {
        var check = new OkHealthCheck();
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("ok", check, failureStatus: null, tags: [])
        };

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => check.CheckHealthAsync(context));

        var results = await Task.WhenAll(tasks);

        Assert.All(results, r => Assert.Equal(HealthStatus.Healthy, r.Status));
    }
}
