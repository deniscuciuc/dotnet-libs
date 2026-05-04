namespace DenisCuciuc.Platform.MQ;

[AttributeUsage(AttributeTargets.Class)]
public sealed class RabbitMqConsumerAttribute : Attribute
{
    public RabbitMqConsumerAttribute(string queue)
    {
        if (string.IsNullOrWhiteSpace(queue))
            throw new ArgumentException("Queue name must be provided.", nameof(queue));

        Queue = queue;
    }

    public string Queue { get; }

    // 0 means use global endpoint default.
    public int PrefetchCount { get; set; }

    // -1 means use global endpoint default.
    public int ConcurrentMessageLimit { get; set; } = -1;

    public bool Durable { get; set; } = true;

    public bool AutoDelete { get; set; }

    // -1 means use global endpoint default.
    public int RetryCount { get; set; } = -1;

    // -1 means use global endpoint default.
    public double RetryIntervalInSeconds { get; set; } = -1;

    // -1 means use global endpoint default.
    public int ConsumerTimeoutInSeconds { get; set; } = -1;

    public RabbitMqQueueType QueueType { get; set; } = RabbitMqQueueType.Inherited;

    // -1 means use global endpoint default.
    public int DeliveryLimit { get; set; } = -1;
}
