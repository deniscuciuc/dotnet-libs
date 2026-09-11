using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Channels;
using CoreLibs.LiveConfig.Startup;
using CoreLibs.Redis;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace CoreLibs.LiveConfig.Hosting;

/// <summary>
/// Background service that subscribes to Redis pub/sub for config update notifications
/// and applies them via the LiveConfigEngine.
/// Uses a <see cref="Channel{T}"/> for backpressure-safe async processing.
/// Automatically reconnects and reconciles on Redis connection loss.
/// Uses <see cref="IRedisClient"/> from CoreLibs.Redis for connection management.
/// </summary>
public sealed class LiveConfigSubscriberService(
    IRedisClient redisClient,
    LiveConfigEngine engine,
    LiveConfigConsumerState consumerState,
    IOptions<LiveConfigHostOptions> options,
    ILogger<LiveConfigSubscriberService> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions NotificationJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly Channel<ConfigUpdateNotification> _channel =
        Channel.CreateBounded<ConfigUpdateNotification>(new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        });

    private readonly ConcurrentDictionary<string, int> _lastProcessedVersions = new(StringComparer.OrdinalIgnoreCase);
    private volatile bool _needsReconciliation;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait for initial load to complete before subscribing
        logger.LogInformation("Waiting for initial config load before subscribing...");
        await consumerState.WaitForInitialLoadAsync(stoppingToken);

        var channelName = options.Value.RedisNotificationChannel;

        // Register connection event handlers for automatic reconnection
        redisClient.Connection.ConnectionRestored += OnConnectionRestored;
        redisClient.Connection.ConnectionFailed += OnConnectionFailed;

        try
        {
            await SubscribeAsync(channelName);
            await ReconcileAsync("startup", stoppingToken);

            // Process notifications and handle reconciliation
            await foreach (var notification in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                // Check if reconciliation is needed (after reconnect)
                if (_needsReconciliation)
                {
                    _needsReconciliation = false;
                    await ReconcileAsync("reconnect", stoppingToken);

                    // Re-subscribe after reconciliation since the connection may have changed
                    await SubscribeAsync(channelName);
                }

                try
                {
                    logger.LogInformation(
                        "Processing config update: {ConfigType} v{Version} (rollback={IsRollback})",
                        notification.ConfigType, notification.Version, notification.IsRollback);

                    await engine.LoadAndApplyAsync(notification.ConfigType, stoppingToken);
                    _lastProcessedVersions[notification.ConfigType] = notification.Version;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error applying config update for {ConfigType}",
                        notification.ConfigType);
                }
            }
        }
        finally
        {
            redisClient.Connection.ConnectionRestored -= OnConnectionRestored;
            redisClient.Connection.ConnectionFailed -= OnConnectionFailed;
        }
    }

    private async Task SubscribeAsync(string channelName)
    {
        var subscriber = redisClient.Connection.GetSubscriber();

        await subscriber.SubscribeAsync(RedisChannel.Literal(channelName), (_, message) =>
        {
            try
            {
                var notification = JsonSerializer.Deserialize<ConfigUpdateNotification>(
                    (string)message!, NotificationJsonOptions);

                if (notification is not null)
                    _channel.Writer.TryWrite(notification);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to deserialize config update notification");
            }
        });

        logger.LogInformation("Subscribed to config updates on channel '{Channel}'", channelName);
    }

    private void OnConnectionFailed(object? sender, ConnectionFailedEventArgs e)
    {
        logger.LogWarning("Redis connection lost ({FailureType}): {Endpoint}. Config updates paused",
            e.FailureType, e.EndPoint);
    }

    private void OnConnectionRestored(object? sender, ConnectionFailedEventArgs e)
    {
        logger.LogInformation("Redis connection restored: {Endpoint}. Triggering reconciliation", e.EndPoint);
        _needsReconciliation = true;

        // Write a synthetic notification to wake up the processing loop
        _channel.Writer.TryWrite(new ConfigUpdateNotification("__reconcile__", 0, false));
    }

    /// <summary>
    /// Reconciles local config versions with the store.
    /// Detects configs that diverged while the subscriber was offline or still warming up.
    /// </summary>
    private async Task ReconcileAsync(string reason, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Reconciling config versions after {Reason}...", reason);

            var manifest = await engine.GetManifestAsync(cancellationToken);

            foreach (var (configType, storeVersion) in manifest)
            {
                _lastProcessedVersions.TryGetValue(configType, out var localVersion);
                if (storeVersion > localVersion)
                {
                    logger.LogInformation(
                        "Reconciliation: {ConfigType} diverged (local v{Local}, store v{Store}). Re-applying",
                        configType, localVersion, storeVersion);

                    await engine.LoadAndApplyAsync(configType, cancellationToken);
                    _lastProcessedVersions[configType] = storeVersion;
                }
            }

            logger.LogInformation("Reconciliation complete after {Reason}", reason);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Reconciliation failed after {Reason}. Some configs may be stale until next update",
                reason);
        }
    }
}
