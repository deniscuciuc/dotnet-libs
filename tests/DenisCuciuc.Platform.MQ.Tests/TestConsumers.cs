using MassTransit;

namespace DenisCuciuc.Platform.MQ.Tests;

[RabbitMqConsumer("test-queue")]
public class AttributedConsumer : IConsumer<TestMessage>
{
    public Task Consume(ConsumeContext<TestMessage> context) => Task.CompletedTask;
}

public class NonAttributedConsumer : IConsumer<TestMessage>
{
    public Task Consume(ConsumeContext<TestMessage> context) => Task.CompletedTask;
}

public record TestMessage(string Value);
