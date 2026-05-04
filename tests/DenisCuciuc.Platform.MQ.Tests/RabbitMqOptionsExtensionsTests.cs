using Microsoft.Extensions.Configuration;

namespace DenisCuciuc.Platform.MQ.Tests;

public class RabbitMqOptionsExtensionsTests
{
    [Fact]
    public void GetQGRabbitMqOptions_ThrowsOnMissingSection()
    {
        var config = new ConfigurationBuilder().Build();

        Assert.Throws<InvalidOperationException>(() => config.GetQGRabbitMqOptions());
    }

    [Fact]
    public void GetQGRabbitMqOptions_ThrowsOnNullConfiguration()
    {
        IConfiguration config = null!;
        Assert.Throws<ArgumentNullException>(() => config.GetQGRabbitMqOptions());
    }

    [Fact]
    public void GetQGRabbitMqOptions_ReturnsOptions_WhenSectionExists()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MQ:Rabbit:Host"] = "amqp://localhost",
                ["MQ:Rabbit:VirtualHost"] = "/",
                ["MQ:Rabbit:Username"] = "guest",
                ["MQ:Rabbit:Password"] = "guest"
            })
            .Build();

        var options = config.GetQGRabbitMqOptions();

        Assert.Equal("amqp://localhost", options.Host);
        Assert.Equal("/", options.VirtualHost);
        Assert.Equal("guest", options.Username);
        Assert.Equal("guest", options.Password);
    }
}
