using Microsoft.Extensions.DependencyInjection;

namespace DenisCuciuc.Platform.Jobs.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddPlatformJobsInMemory_RegistersInMemoryScheduler()
    {
        var services = new ServiceCollection();

        services.AddPlatformJobsInMemory();

        var provider = services.BuildServiceProvider();
        var scheduler = provider.GetRequiredService<IJobScheduler>();

        Assert.IsType<InMemoryJobScheduler>(scheduler);
    }

    [Fact]
    public void AddPlatformJobsInMemory_RegistersBothInterfaces()
    {
        var services = new ServiceCollection();

        services.AddPlatformJobsInMemory();

        var provider = services.BuildServiceProvider();
        var scheduler = provider.GetRequiredService<IJobScheduler>();
        var concrete = provider.GetRequiredService<InMemoryJobScheduler>();

        Assert.Same(scheduler, concrete);
    }
}
