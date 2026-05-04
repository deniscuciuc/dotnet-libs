using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DenisCuciuc.Platform.MQ.Tests;

public class MqConsumerConfiguratorTests
{
    private static IConfiguration BuildConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MQ:Rabbit:Host"] = "amqp://localhost",
                ["MQ:Rabbit:VirtualHost"] = "/",
                ["MQ:Rabbit:Username"] = "guest",
                ["MQ:Rabbit:Password"] = "guest"
            })
            .Build();

    [Fact]
    public void AddPlatformRabbitMq_RegistersMassTransit()
    {
        var services = new ServiceCollection();
        var config = BuildConfig();

        services.AddPlatformRabbitMq(config);

        // IBus should be registered by MassTransit (without resolving/starting it)
        Assert.Contains(services, d => d.ServiceType == typeof(IBus));
    }

    [Fact]
    public void AddPlatformRabbitMq_WithConsumer_RegistersConsumer()
    {
        var services = new ServiceCollection();
        var config = BuildConfig();

        services.AddPlatformRabbitMq(config, c => c.AddConsumer<AttributedConsumer>());

        // Consumer class should be registered in the service collection
        Assert.Contains(services, d => d.ServiceType == typeof(AttributedConsumer));
    }

    [Fact]
    public void AddPlatformRabbitMq_WithNoConsumers_DoesNotThrow()
    {
        var services = new ServiceCollection();
        var config = BuildConfig();

        var ex = Record.Exception(() => services.AddPlatformRabbitMq(config));
        Assert.Null(ex);
    }

    [Fact]
    public void AddPlatformRabbitMq_ThrowsOnNullConfig()
    {
        var services = new ServiceCollection();
        IConfiguration config = null!;

        Assert.Throws<NullReferenceException>(() =>
            services.AddPlatformRabbitMq(config));
    }
}
