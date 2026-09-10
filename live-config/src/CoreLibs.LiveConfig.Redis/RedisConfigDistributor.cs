using System.Text.Json;
using CoreLibs.Redis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace CoreLibs.LiveConfig.Redis;

/// <summary>
/// Redis-backed implementation of <see cref="IConfigDistributor"/>.
/// Stores config data in Redis hashes and publishes update notifications via pub/sub.
/// Uses <see cref="IRedisClient"/> from CoreLibs.Redis for connection management.
/// </summary>
public sealed class RedisConfigDistributor(
    IRedisClient redisClient,
    IOptions<RedisLiveConfigOptions> options,
    ILogger<RedisConfigDistributor> logger) : IConfigDistributor
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    private string DataKey(string configType)
    {
        return $"{options.Value.KeyPrefix}:{configType}";
    }

    private string VersionKey(string configType)
    {
        return $"{options.Value.KeyPrefix}:{configType}:version";
    }

    private string ManifestKey => $"{options.Value.KeyPrefix}:manifest";

    public async Task SetAsync(string configType, string dataJson, int version,
        CancellationToken cancellationToken = default)
    {
        var db = redisClient.Database;
        var batch = db.CreateBatch();

        var dataKey = DataKey(configType);
        var versionKey = VersionKey(configType);
        var opts = options.Value;

        var expiry = opts.Expiration;

        var tasks = new List<Task>
        {
            batch.StringSetAsync(dataKey, dataJson, expiry, When.Always),
            batch.StringSetAsync(versionKey, version, expiry, When.Always),
            batch.HashSetAsync(ManifestKey, configType, version)
        };

        batch.Execute();
        await Task.WhenAll(tasks);

        logger.LogDebug("Set config {ConfigType} v{Version} in Redis", configType, version);
    }

    public async Task<ConfigDistributorEntry?> GetAsync(string configType,
        CancellationToken cancellationToken = default)
    {
        var db = redisClient.Database;

        var dataTask = db.StringGetAsync(DataKey(configType));
        var versionTask = db.StringGetAsync(VersionKey(configType));

        var data = await dataTask;
        var version = await versionTask;

        if (data.IsNullOrEmpty || version.IsNullOrEmpty)
            return null;

        return new ConfigDistributorEntry(data!, (int)version);
    }

    public async Task DeleteAsync(string configType, CancellationToken cancellationToken = default)
    {
        var db = redisClient.Database;

        var batch = db.CreateBatch();
        var tasks = new List<Task>
        {
            batch.KeyDeleteAsync(DataKey(configType)),
            batch.KeyDeleteAsync(VersionKey(configType)),
            batch.HashDeleteAsync(ManifestKey, configType)
        };

        batch.Execute();
        await Task.WhenAll(tasks);

        logger.LogInformation("Deleted config {ConfigType} from Redis", configType);
    }

    public async Task PublishUpdateAsync(ConfigUpdateNotification notification,
        CancellationToken cancellationToken = default)
    {
        var subscriber = redisClient.Connection.GetSubscriber();
        var json = JsonSerializer.Serialize(notification, JsonOptions);
        await subscriber.PublishAsync(RedisChannel.Literal(options.Value.NotificationChannel), json);

        logger.LogDebug("Published update notification for {ConfigType} v{Version}", notification.ConfigType,
            notification.Version);
    }

    public async Task SubscribeAsync(Func<ConfigUpdateNotification, Task> handler,
        CancellationToken cancellationToken = default)
    {
        var subscriber = redisClient.Connection.GetSubscriber();
        var channel = RedisChannel.Literal(options.Value.NotificationChannel);

        await subscriber.SubscribeAsync(channel, async (_, message) =>
        {
            try
            {
                var notification = JsonSerializer.Deserialize<ConfigUpdateNotification>((string)message!, JsonOptions);
                if (notification is not null)
                    await handler(notification);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing config update notification");
            }
        });

        logger.LogInformation("Subscribed to config update notifications on channel {Channel}",
            options.Value.NotificationChannel);
    }

    public async Task<IReadOnlyDictionary<string, int>> GetManifestAsync(CancellationToken cancellationToken = default)
    {
        var db = redisClient.Database;
        var entries = await db.HashGetAllAsync(ManifestKey);

        return entries.ToDictionary(
            e => e.Name.ToString(),
            e => (int)e.Value);
    }
}
