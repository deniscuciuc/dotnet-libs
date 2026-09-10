namespace CoreLibs.LiveConfig.Redis;

/// <summary>
/// Options for the Redis config distributor.
/// </summary>
public sealed class RedisLiveConfigOptions
{
    public const string SectionPath = "LiveConfig:Redis";

    /// <summary>
    /// Key prefix for config data stored in Redis.
    /// </summary>
    public string KeyPrefix { get; set; } = "liveconfig";

    /// <summary>
    /// Redis pub/sub channel name for update notifications.
    /// </summary>
    public string NotificationChannel { get; set; } = "liveconfig:updated";

    /// <summary>
    /// TTL for config entries in Redis. Null means no expiration.
    /// </summary>
    public TimeSpan? Expiration { get; set; }
}
