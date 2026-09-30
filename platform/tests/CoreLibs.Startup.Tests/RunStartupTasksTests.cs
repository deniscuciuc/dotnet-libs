using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CoreLibs.Startup.Tests;

public class RunStartupTasksTests
{
    [Fact]
    public async Task RunStartupTasksAsync_RunsTasksInOrder_WithoutStartingTheHost()
    {
        var order = new List<string>();
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton(order);
        builder.Services.AddCoreStartup<SecondTask>();
        builder.Services.AddCoreStartup<FirstTask>();
        using var host = builder.Build();
        var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

        await host.RunStartupTasksAsync();

        Assert.Equal(["first", "second"], order);
        Assert.False(lifetime.ApplicationStarted.IsCancellationRequested);
    }

    [Fact]
    public async Task RunStartupTasksAsync_FailsFast_WhenAGuardIsNotConfigured()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddCoreStartupGuard<RunStartupTasksTests>();
        using var host = builder.Build();

        await Assert.ThrowsAsync<InvalidOperationException>(() => host.RunStartupTasksAsync());
    }

    private sealed class FirstTask(List<string> order) : IStartup
    {
        public int Order => 1;

        public Task StartupAsync(IHostStartup host, CancellationToken cancellationToken = default)
        {
            order.Add("first");
            return Task.CompletedTask;
        }
    }

    private sealed class SecondTask(List<string> order) : IStartup
    {
        public int Order => 2;

        public Task StartupAsync(IHostStartup host, CancellationToken cancellationToken = default)
        {
            order.Add("second");
            return Task.CompletedTask;
        }
    }
}
