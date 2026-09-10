using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CoreLibs.HealthCheck.Tests;

public class OkHealthCheckBuilderExtensionsTests
{
    [Fact]
    public void AddOkHealthCheck_RegistersCheckWithDefaultName()
    {
        var services = new ServiceCollection();
        services.AddHealthChecks().AddOkHealthCheck();

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<HealthCheckServiceOptions>>();

        var registration = options.Value.Registrations.FirstOrDefault(r => r.Name == "ok");
        Assert.NotNull(registration);
    }

    [Fact]
    public void AddOkHealthCheck_TagsAsLiveness()
    {
        var services = new ServiceCollection();
        services.AddHealthChecks().AddOkHealthCheck();

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<HealthCheckServiceOptions>>();

        var registration = options.Value.Registrations.First(r => r.Name == "ok");
        Assert.Contains("liveness", registration.Tags);
    }

    [Fact]
    public void AddOkHealthCheck_AcceptsCustomName()
    {
        var services = new ServiceCollection();
        services.AddHealthChecks().AddOkHealthCheck("liveness");

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<HealthCheckServiceOptions>>();

        var registration = options.Value.Registrations.FirstOrDefault(r => r.Name == "liveness");
        Assert.NotNull(registration);
    }
}
