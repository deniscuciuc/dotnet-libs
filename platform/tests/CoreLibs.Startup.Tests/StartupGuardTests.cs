using Microsoft.Extensions.DependencyInjection;

namespace CoreLibs.Startup.Tests;

public class StartupGuardTests
{
    [Fact]
    public async Task Unconfigured_ThrowsInvalidOperationException()
    {
        var guard = new StartupGuard<FakeComponent>();
        var host = CreateHost();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => guard.StartupAsync(host));

        Assert.Contains("FakeComponent", ex.Message);
    }

    [Fact]
    public async Task Configured_DoesNotThrow()
    {
        var guard = new StartupGuard<FakeComponent>();
        guard.Configure();

        var host = CreateHost();
        await guard.StartupAsync(host);
    }

    [Fact]
    public void CustomMessage_IsUsed()
    {
        var guard = new StartupGuard<FakeComponent>("custom error");

        Assert.Equal("custom error", guard.Message);
    }

    [Fact]
    public void Order_IsIntMaxValue()
    {
        var guard = new StartupGuard<FakeComponent>();

        Assert.Equal(int.MaxValue, guard.Order);
    }

    private class FakeComponent;

    private static IHostStartup CreateHost()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        return new FakeHostStartup(services);
    }
}
