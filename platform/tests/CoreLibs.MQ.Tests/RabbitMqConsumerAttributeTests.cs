namespace CoreLibs.MQ.Tests;

public class RabbitMqConsumerAttributeTests
{
    [Fact]
    public void Constructor_SetsQueueName()
    {
        var attr = new RabbitMqConsumerAttribute("my-queue");
        Assert.Equal("my-queue", attr.Queue);
    }

    [Fact]
    public void Constructor_ThrowsOnEmptyQueue()
    {
        var ex = Assert.Throws<ArgumentException>(() => new RabbitMqConsumerAttribute(""));
        Assert.Contains("queue", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_ThrowsOnWhitespaceQueue()
    {
        Assert.Throws<ArgumentException>(() => new RabbitMqConsumerAttribute("   "));
    }

    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var attr = new RabbitMqConsumerAttribute("q");

        Assert.Equal(0, attr.PrefetchCount);
        Assert.Equal(-1, attr.ConcurrentMessageLimit);
        Assert.True(attr.Durable);
        Assert.False(attr.AutoDelete);
        Assert.Equal(-1, attr.RetryCount);
        Assert.Equal(-1, attr.RetryIntervalInSeconds);
        Assert.Equal(-1, attr.ConsumerTimeoutInSeconds);
        Assert.Equal(RabbitMqQueueType.Inherited, attr.QueueType);
        Assert.Equal(-1, attr.DeliveryLimit);
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        var attr = new RabbitMqConsumerAttribute("q")
        {
            PrefetchCount = 5,
            ConcurrentMessageLimit = 3,
            Durable = false,
            AutoDelete = true,
            RetryCount = 2,
            RetryIntervalInSeconds = 10,
            ConsumerTimeoutInSeconds = 30,
            QueueType = RabbitMqQueueType.Quorum,
            DeliveryLimit = 5
        };

        Assert.Equal(5, attr.PrefetchCount);
        Assert.Equal(3, attr.ConcurrentMessageLimit);
        Assert.False(attr.Durable);
        Assert.True(attr.AutoDelete);
        Assert.Equal(2, attr.RetryCount);
        Assert.Equal(10, attr.RetryIntervalInSeconds);
        Assert.Equal(30, attr.ConsumerTimeoutInSeconds);
        Assert.Equal(RabbitMqQueueType.Quorum, attr.QueueType);
        Assert.Equal(5, attr.DeliveryLimit);
    }

    [Fact]
    public void AttributeTarget_IsClass()
    {
        var usageAttr = typeof(RabbitMqConsumerAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .FirstOrDefault();

        Assert.NotNull(usageAttr);
        Assert.Equal(AttributeTargets.Class, usageAttr.ValidOn);
    }
}
