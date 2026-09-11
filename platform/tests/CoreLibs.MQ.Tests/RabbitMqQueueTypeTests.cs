namespace CoreLibs.MQ.Tests;

public class RabbitMqQueueTypeTests
{
    [Fact]
    public void AllValues_AreDefined()
    {
        var values = Enum.GetValues<RabbitMqQueueType>();
        Assert.Contains(RabbitMqQueueType.Inherited, values);
        Assert.Contains(RabbitMqQueueType.Quorum, values);
        Assert.Contains(RabbitMqQueueType.Classic, values);
    }
}
