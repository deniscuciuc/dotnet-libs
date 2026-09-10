# Hosting & Deployment

The `CoreLibs.LiveConfig.Hosting` package provides the infrastructure to run a deployable config manager service — MassTransit consumers for import/rollback commands and a Redis subscriber for live updates.

## Setup

```csharp
// Register hosting services
builder.Services.AddLiveConfigHosting(builder.Configuration);

// Add MQ consumers to MassTransit
builder.Services.AddMassTransit(x =>
{
    x.AddLiveConfigConsumers();
    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMq:Host"]);
        ctx.ConfigureLiveConfigEndpoints(cfg);
    });
});
```

## Configuration

```yaml
liveconfig:
  hosting:
    redisNotificationChannel: "liveconfig:updated"
    importQueueName: liveconfig-import
    rollbackQueueName: liveconfig-rollback
    enableMqConsumers: true
    enableRedisSubscriber: true
```

| Option | Default | Description |
|---|---|---|
| `RedisNotificationChannel` | `liveconfig:updated` | Redis pub/sub channel |
| `ImportQueueName` | `liveconfig-import` | RabbitMQ queue for import commands |
| `RollbackQueueName` | `liveconfig-rollback` | RabbitMQ queue for rollback commands |
| `EnableMqConsumers` | `true` | Register MassTransit consumers |
| `EnableRedisSubscriber` | `true` | Start Redis subscriber background service |

## MQ Contracts

### ConfigImportRequest

Triggers a config import. Send from any service (e.g. admin API, Telegram bot):

```csharp
await publishEndpoint.Publish(new ConfigImportRequest
{
    ImporterName = "Bonuses",    // null = run all importers
    RequestedBy = "admin-api"
});
```

### ConfigImportCompleted

Published after import completes:

```csharp
public sealed record ConfigImportCompleted
{
    public required IReadOnlyList<ConfigImportResult> Results { get; init; }
    public string? RequestedBy { get; init; }
    public DateTimeOffset CompletedAt { get; init; }
}
```

### ConfigRollbackRequest

Triggers a version rollback:

```csharp
await publishEndpoint.Publish(new ConfigRollbackRequest
{
    ConfigType = "bonuses",
    TargetVersion = 3,
    RequestedBy = "admin"
});
```

### ConfigRollbackCompleted

Published after rollback:

```csharp
public sealed record ConfigRollbackCompleted
{
    public required string ConfigType { get; init; }
    public int Version { get; init; }
    public bool Success { get; init; }
    public string? Error { get; init; }
}
```

## Background Services

### LiveConfigSubscriberService

Subscribes to Redis pub/sub and applies updates:

1. Waits for initial config preload to complete (`LiveConfigConsumerState`)
2. Subscribes to the configured Redis channel
3. Deserializes `ConfigUpdateNotification` messages
4. Calls `engine.LoadAndApplyAsync(configType)` for each notification
5. Uses `Channel<T>` for backpressure-safe async processing

### LiveConfigPreloadService

Runs at startup, before the app accepts traffic:

1. Loads all `CriticalConfigTypes` from the store/distributor
2. Applies them to typed caches
3. Signals `LiveConfigConsumerState` that initial load is complete
4. Optionally starts a polling loop for environments without pub/sub

## Deployment Architecture

### Config Manager Service

The dedicated service that owns the config pipeline:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLiveConfig(builder.Configuration);
builder.Services.AddLiveConfigGSheet(builder.Configuration);
builder.Services.AddGSheetImportersInAssemblyOf<Program>();
builder.Services.AddLiveConfigMongoStore(builder.Configuration);
builder.Services.AddLiveConfigRedis(builder.Configuration);
builder.Services.AddLiveConfigHosting(builder.Configuration);
builder.Services.AddLiveConfigPreloadService();

builder.Services.AddMassTransit(x =>
{
    x.AddLiveConfigConsumers();
    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMq:Host"]);
        ctx.ConfigureLiveConfigEndpoints(cfg);
    });
});

var app = builder.Build();
await app.RunAsync();
```

### Consumer Service

Any service that reads live configs:

```csharp
builder.Services.AddLiveConfig(builder.Configuration);
builder.Services.AddLiveConfigCachedConfig<BonusConfig>("bonuses");
builder.Services.AddLiveConfigRedis(builder.Configuration);
builder.Services.AddLiveConfigPreloadService();
builder.Services.AddLiveConfigHosting(builder.Configuration);
```

No MQ consumers needed — just the Redis subscriber for live updates.
