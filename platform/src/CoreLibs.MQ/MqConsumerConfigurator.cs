using System.Reflection;
using MassTransit;

namespace CoreLibs.MQ;

public sealed class MqConsumerConfigurator(
    IBusRegistrationConfigurator configurator
) : IMqConsumerConfigurator
{
    private readonly Dictionary<Type, RabbitMqConsumerAttribute> _maps = new();

    public void Configure(
        IBusRegistrationContext ctx,
        IRabbitMqBusFactoryConfigurator cfg,
        CoreRabbitMqOptions options)
    {
        var defaults = options.EndpointDefaults;

        foreach (var pair in _maps)
        {
            var attribute = pair.Value;
            if (!string.IsNullOrWhiteSpace(attribute.Queue))
                cfg.ReceiveEndpoint(attribute.Queue, e =>
                {
                    e.ConfigureConsumeTopology = true;
                    e.PrefetchCount = attribute.PrefetchCount > 0
                        ? (ushort)attribute.PrefetchCount
                        : defaults.PrefetchCount;

                    var concurrentLimit = attribute.ConcurrentMessageLimit >= 0
                        ? attribute.ConcurrentMessageLimit
                        : defaults.ConcurrentMessageLimit;
                    if (concurrentLimit > 0)
                        e.ConcurrentMessageLimit = concurrentLimit;

                    e.Durable = attribute.Durable;
                    e.AutoDelete = attribute.AutoDelete;

                    var queueType = attribute.QueueType != RabbitMqQueueType.Inherited
                        ? attribute.QueueType
                        : defaults.QueueType;

                    if (queueType == RabbitMqQueueType.Quorum)
                    {
                        e.SetQuorumQueue();

                        var deliveryLimit = attribute.DeliveryLimit >= 0
                            ? attribute.DeliveryLimit
                            : defaults.DeliveryLimit;

                        if (deliveryLimit > 0)
                            e.SetQueueArgument("x-delivery-limit", deliveryLimit);
                    }

                    var retryCount = attribute.RetryCount >= 0
                        ? attribute.RetryCount
                        : defaults.RetryCount;
                    var retryIntervalInSeconds = attribute.RetryIntervalInSeconds >= 0
                        ? attribute.RetryIntervalInSeconds
                        : defaults.RetryIntervalInSeconds;

                    if (retryCount > 0)
                        e.UseMessageRetry(a =>
                            a.Interval(retryCount, TimeSpan.FromSeconds(retryIntervalInSeconds))
                        );

                    var timeoutInSeconds = attribute.ConsumerTimeoutInSeconds >= 0
                        ? attribute.ConsumerTimeoutInSeconds
                        : defaults.ConsumerTimeoutInSeconds;
                    if (timeoutInSeconds > 0)
                        e.UseTimeout(t => t.Timeout = TimeSpan.FromSeconds(timeoutInSeconds));

                    e.ConfigureConsumer(ctx, pair.Key);
                });
        }
    }

    public IMqConsumerConfigurator AddConsumer<T>() where T : class, IConsumer
    {
        if (typeof(T).GetCustomAttribute(typeof(RabbitMqConsumerAttribute)) is RabbitMqConsumerAttribute attribute)
        {
            // Retry is configured on the receive endpoint (Configure method), not here,
            // to avoid compounding retries (endpoint Ã— consumer = RetryCountÂ²).
            var consumerDef = configurator.AddConsumer<T>();
            consumerDef.ExcludeFromConfigureEndpoints();

            if (!_maps.TryAdd(typeof(T), attribute))
                throw new InvalidOperationException($"Consumer '{typeof(T).Name}' is already registered.");

        }
        else
        {
            configurator.AddConsumer<T>();
        }

        return this;
    }
}
