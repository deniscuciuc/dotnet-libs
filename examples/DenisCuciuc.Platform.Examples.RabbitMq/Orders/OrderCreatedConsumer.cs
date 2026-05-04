using DenisCuciuc.Platform.MQ;
using MassTransit;

namespace DenisCuciuc.Platform.Examples.RabbitMq.Orders;

/// <summary>
/// Receives <see cref="OrderCreatedEvent"/> messages from the <c>orders.created</c> queue
/// and logs order details. Retries 3 times with a 5-second interval on failure.
/// </summary>
[RabbitMqConsumer("orders.created", RetryCount = 3, RetryIntervalInSeconds = 5)]
public sealed class OrderCreatedConsumer(ILogger<OrderCreatedConsumer> logger)
    : IConsumer<OrderCreatedEvent>
{
    public Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        var msg = context.Message;

        logger.LogInformation(
            "Order received â€” Id: {OrderId} | Customer: {CustomerName} | Items: {Items} | Total: {Total:C}",
            msg.OrderId,
            msg.CustomerName,
            string.Join(", ", msg.Items),
            msg.Total);

        return Task.CompletedTask;
    }
}
