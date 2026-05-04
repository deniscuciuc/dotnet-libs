namespace DenisCuciuc.Platform.Examples.RabbitMq.Orders;

/// <summary>
/// Shared message contract. Both the API (publisher) and the consumer reference this type.
/// MassTransit uses the full type name to route to the correct RabbitMQ exchange.
/// </summary>
public record OrderCreatedEvent(
    Guid OrderId,
    string CustomerName,
    string[] Items,
    decimal Total);
