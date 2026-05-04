using DenisCuciuc.Platform.Configuration;
using DenisCuciuc.Platform.Examples.RabbitMq.Orders;
using DenisCuciuc.Platform.HealthCheck;
using DenisCuciuc.Platform.Metrics;
using DenisCuciuc.Platform.MQ;
using DenisCuciuc.Platform.Serilog;
using DenisCuciuc.Platform.Serilog.Configuration;
using DenisCuciuc.Platform.Startup;
using DenisCuciuc.Platform.Swagger.Theme;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

// â”€â”€ Configuration â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
builder.AddPlatformConfiguration(o => o.Profile = ConfigurationProfile.Default);

// â”€â”€ Observability â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
builder.Host.UsePlatformSerilog(o => o.Profile = SerilogProfile.Worker);

// â”€â”€ Messaging â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
// Registers MassTransit with RabbitMQ.
// IPublishEndpoint and IBus are available for publishing.
// OrderCreatedConsumer listens on the "orders.created" queue.
builder.Services.AddPlatformRabbitMq(
    builder.Configuration,
    consumers => consumers.AddConsumer<OrderCreatedConsumer>());

// â”€â”€ API â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// â”€â”€ Health & Metrics â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
builder.Services.AddPlatformHealthChecks();
builder.Services.AddPlatformMetrics(builder.Configuration);

var app = builder.Build();

// â”€â”€ Middleware â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
app.UsePlatformHealthChecks();
app.MapPlatformMetrics();
app.UsePlatformSwagger();

// Creates an order and dispatches an OrderCreatedEvent to RabbitMQ.
app.MapPost("/orders", async (CreateOrderRequest request, IPublishEndpoint publisher) =>
    {
        var orderId = Guid.NewGuid();

        await publisher.Publish(new OrderCreatedEvent(
            orderId,
            request.CustomerName,
            request.Items,
            request.Total));

        return Results.Accepted($"/orders/{orderId}", new { orderId });
    })
    .WithName("CreateOrder")
    .WithSummary("Create an order")
    .WithDescription("Publishes an OrderCreatedEvent to RabbitMQ. The consumer logs the event.");

// â”€â”€ Start â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
await app.RunWithStartupAsync();

// â”€â”€ Request model â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
public record CreateOrderRequest(string CustomerName, string[] Items, decimal Total);
