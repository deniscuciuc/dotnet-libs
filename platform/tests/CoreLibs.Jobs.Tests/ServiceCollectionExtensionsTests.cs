using Microsoft.Extensions.DependencyInjection;

namespace CoreLibs.Jobs.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddCoreJobsInMemory_RegistersInMemoryScheduler()
    {
        var services = new ServiceCollection();

        services.AddCoreJobsInMemory();

        var provider = services.BuildServiceProvider();
        var scheduler = provider.GetRequiredService<IJobScheduler>();

        Assert.IsType<InMemoryJobScheduler>(scheduler);
    }

    [Fact]
    public void AddCoreJobsInMemory_RegistersBothInterfaces()
    {
        var services = new ServiceCollection();

        services.AddCoreJobsInMemory();

        var provider = services.BuildServiceProvider();
        var scheduler = provider.GetRequiredService<IJobScheduler>();
        var concrete = provider.GetRequiredService<InMemoryJobScheduler>();

        Assert.Same(scheduler, concrete);
    }
}
