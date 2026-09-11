using Microsoft.Extensions.DependencyInjection;

namespace CoreLibs.Startup.Tests;

public class MarkConfiguredTests
{
    [Fact]
    public async Task MarkConfigured_ViaServiceProvider_PreventsThrow()
    {
        var services = new ServiceCollection();
        services.AddCoreStartupGuard<MyService>();
        var provider = services.BuildServiceProvider();

        provider.MarkConfigured<MyService>();

        var guard = provider.GetRequiredService<StartupGuard<MyService>>();
        var host = new FakeHostStartup(provider);
        await guard.StartupAsync(host);
    }

    [Fact]
    public void AddCoreStartupGuard_RegistersAsIStartup()
    {
        var services = new ServiceCollection();
        services.AddCoreStartupGuard<MyService>();
        var provider = services.BuildServiceProvider();

        var startup = provider.GetService<IStartup>();
        Assert.NotNull(startup);
        Assert.IsType<StartupGuard<MyService>>(startup);
    }

    [Fact]
    public void AddCoreStartup_RegistersConcreteAndInterface()
    {
        var services = new ServiceCollection();
        services.AddCoreStartup<SampleStartupTask>();
        var provider = services.BuildServiceProvider();

        var concrete = provider.GetService<SampleStartupTask>();
        var iface = provider.GetService<IStartup>();

        Assert.NotNull(concrete);
        Assert.NotNull(iface);
        Assert.Same(concrete, iface);
    }

    private class MyService;
}
