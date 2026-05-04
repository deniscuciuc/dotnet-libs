using MassTransit;
using DenisCuciuc.Platform.MQ;

/// <summary>
/// Example RabbitMQ consumer that processes order-created events.
/// The <see cref="RabbitMqConsumerAttribute"/> configures the queue name and retry behavior.
/// </summary>
[RabbitMqConsumer("order-created", RetryCount = 3, RetryIntervalInSeconds = 5)]
public sealed class OrderCreatedConsumer(ILogger<OrderCreatedConsumer> logger) : IConsumer<OrderCreatedEvent>
{
    public Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        logger.LogInformation(
            "Processing order {OrderId} for {CustomerName} â€” total {Total:C}",
            context.Message.OrderId,
            context.Message.CustomerName,
            context.Message.Total);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Example message contract.
/// </summary>
public record OrderCreatedEvent(Guid OrderId, string CustomerName, decimal Total);
