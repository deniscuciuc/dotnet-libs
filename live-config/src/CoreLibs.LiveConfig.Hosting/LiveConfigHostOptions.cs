namespace CoreLibs.LiveConfig.Hosting;

/// <summary>
/// Options for the LiveConfig hosting infrastructure.
/// </summary>
public sealed class LiveConfigHostOptions
{
    public const string SectionPath = "LiveConfig:Hosting";

    /// <summary>
    /// Redis pub/sub channel name for config update notifications.
    /// </summary>
    public string RedisNotificationChannel { get; set; } = "liveconfig:updated";

    /// <summary>
    /// RabbitMQ queue name for import requests.
    /// </summary>
    public string ImportQueueName { get; set; } = "liveconfig-import";

    /// <summary>
    /// RabbitMQ queue name for rollback requests.
    /// </summary>
    public string RollbackQueueName { get; set; } = "liveconfig-rollback";

    /// <summary>
    /// Enable MQ consumers for import/rollback commands.
    /// </summary>
    public bool EnableMqConsumers { get; set; } = true;

    /// <summary>
    /// Enable Redis subscriber for update notifications.
    /// </summary>
    public bool EnableRedisSubscriber { get; set; } = true;
}
