# Distribution & Redis

The distributor layer provides fast config reads and real-time update notifications across distributed services.

## Interface

```csharp
public interface IConfigDistributor
{
    Task SetAsync(string configType, string dataJson, int version, CancellationToken ct = default);
    Task<ConfigDistributorEntry?> GetAsync(string configType, CancellationToken ct = default);
    Task DeleteAsync(string configType, CancellationToken ct = default);
    Task PublishUpdateAsync(ConfigUpdateNotification notification, CancellationToken ct = default);
    Task SubscribeAsync(Func<ConfigUpdateNotification, Task> handler, CancellationToken ct = default);
    Task<IReadOnlyDictionary<string, int>> GetManifestAsync(CancellationToken ct = default);
}
```

## Redis Distributor (`CoreLibs.LiveConfig.Redis`)

### Setup

```csharp
builder.Services.AddLiveConfigRedis(builder.Configuration);
```

### Configuration

```yaml
liveconfig:
  redis:
    keyPrefix: liveconfig
    notificationChannel: "liveconfig:updated"
    expiration: null   # TimeSpan, null = no expiry
```

| Option | Default | Description |
|---|---|---|
| `KeyPrefix` | `liveconfig` | Redis key prefix for all config data |
| `NotificationChannel` | `liveconfig:updated` | Pub/sub channel for update notifications |
| `Expiration` | `null` | TTL for config entries (null = no expiry) |

### Redis Key Layout

```
liveconfig:{configType}           → JSON data string
liveconfig:{configType}:version   → version number
liveconfig:manifest               → hash { configType: version, ... }
```

### How It Works

1. **Import**: Engine calls `SetAsync` → Redis stores JSON + version + updates manifest hash
2. **Notify**: Engine calls `PublishUpdateAsync` → Redis publishes on `liveconfig:updated` channel
3. **Subscribe**: Consumer services subscribe to the channel → receive `ConfigUpdateNotification`
4. **Apply**: On notification, consumer calls `engine.LoadAndApplyAsync(configType)` → reads from Redis → deserializes into typed cache

### Notification Payload

```csharp
public sealed record ConfigUpdateNotification(
    string ConfigType,
    int Version,
    bool IsRollback);
```

## Optional

The distributor is **optional**. If no `IConfigDistributor` is registered, the `LiveConfigEngine` skips distribution and works in store-only mode. This is suitable for:

- Single-instance deployments
- Development/testing
- Services that only import (no consumers)

## LoadAndApply Flow

When a consumer receives a notification, the engine's `LoadAndApplyAsync` method:

1. Tries the **distributor** first (fast — Redis read)
2. Falls back to the **store** (reliable — MongoDB/Postgres query)
3. **Backfills** the distributor if data was loaded from the store

```csharp
// Called automatically by LiveConfigSubscriberService
// or manually:
await engine.LoadAndApplyAsync("bonuses", cancellationToken);
```
