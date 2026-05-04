using System.ComponentModel.DataAnnotations;

namespace DenisCuciuc.Platform.MQ;

public sealed class PlatformRabbitMqOptions
{
    public const string DefaultSectionPath = "MQ:Rabbit";

    [Required]
    public string Host { get; set; } = string.Empty;

    [Required]
    public string VirtualHost { get; set; } = "/";

    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    public RabbitMqEndpointDefaults EndpointDefaults { get; set; } = new();
}

public sealed class RabbitMqEndpointDefaults
{
    [Range(1, ushort.MaxValue)]
    public ushort PrefetchCount { get; set; } = 1;

    [Range(0, int.MaxValue)]
    public int ConcurrentMessageLimit { get; set; }

    public bool Durable { get; set; } = true;

    public bool AutoDelete { get; set; }

    [Range(0, int.MaxValue)]
    public int RetryCount { get; set; } = 3;

    [Range(0, int.MaxValue)]
    public int RetryIntervalInSeconds { get; set; } = 3;

    [Range(0, int.MaxValue)]
    public int ConsumerTimeoutInSeconds { get; set; }

    public RabbitMqQueueType QueueType { get; set; } = RabbitMqQueueType.Classic;

    [Range(0, int.MaxValue)]
    public int DeliveryLimit { get; set; }
}
