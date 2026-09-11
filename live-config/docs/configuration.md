# Configuration Reference

All LiveConfig options use a hierarchical `LiveConfig` prefix. Options are bound via `IOptions<T>` and support YAML and JSON configuration formats.

## Full Configuration Example

```yaml
liveconfig:
  # Core options
  environment: Production
  criticalConfigTypes:
    - bonuses
    - game-settings
    - translations
  pollingInterval: "00:05:00"
  enablePollingFallback: false
  retry:
    maxRetries: 3
    initialDelay: "00:00:00.250"
    maxDelay: "00:00:05"

  # Store options (pick one)
  store:
    mongodb:
      collectionName: config_snapshots
    postgres:
      schema: public
      tableName: config_snapshots

  # Retention policy
  retention:
    enabled: true
    maxVersions: 30
    maxAge: "7.00:00:00"
    keepActiveAlways: true

  # Redis distributor
  redis:
    keyPrefix: liveconfig
    notificationChannel: "liveconfig:updated"
    expiration: null

  # Hosting infrastructure
  hosting:
    redisNotificationChannel: "liveconfig:updated"
    importQueueName: liveconfig-import
    rollbackQueueName: liveconfig-rollback
    enableMqConsumers: true
    enableRedisSubscriber: true

# Google Sheets source (top-level section)
GSheet:
  spreadsheetId: "1ABC123_your_spreadsheet_id"
  applicationName: "LiveConfigManager"
  failOnImportErrors: true
  failOnValidationErrors: false
  credentials:
    method: CredentialsBase64Env
    value: GOOGLE_SHEET_CREDENTIALS

# JSON source
liveconfig:
  json:
    directory: "./configs"
    environment: Production
    searchPattern: "*.json"
```

## Options Classes

### LiveConfigOptions

Section: `LiveConfig`

| Property | Type | Default | Description |
|---|---|---|---|
| `Environment` | `string?` | `null` | Environment name (auto-detected from `IHostEnvironment` if unset) |
| `CriticalConfigTypes` | `HashSet<string>` | `[]` | Config types that must load before the app accepts traffic |
| `PollingInterval` | `TimeSpan` | `00:05:00` | Polling interval when pub/sub is unavailable |
| `EnablePollingFallback` | `bool` | `false` | Enable polling when pub/sub is unavailable |
| `Retry.MaxRetries` | `int` | `3` | Max retry attempts for transient failures |
| `Retry.InitialDelay` | `TimeSpan` | `250ms` | Initial retry delay (exponential backoff) |
| `Retry.MaxDelay` | `TimeSpan` | `5s` | Maximum retry delay |

### MongoConfigStoreOptions

Section: `LiveConfig:Store:MongoDB`

| Property | Type | Default | Description |
|---|---|---|---|
| `CollectionName` | `string` | `config_snapshots` | MongoDB collection name |

### PostgresConfigStoreOptions

Section: `LiveConfig:Store:Postgres`

| Property | Type | Default | Description |
|---|---|---|---|
| `Schema` | `string` | `public` | Database schema |
| `TableName` | `string` | `config_snapshots` | Table name |

### ConfigRetentionOptions

Section: `LiveConfig:Retention`

| Property | Type | Default | Description |
|---|---|---|---|
| `Enabled` | `bool` | `true` | Enable retention cleanup |
| `MaxVersions` | `int` | `30` | Max versions to keep per config type (0 = count-based disabled) |
| `MaxAge` | `TimeSpan?` | `null` | Max age of versions to keep (null = age-based disabled) |
| `KeepActiveAlways` | `bool` | `true` | Never delete the active version regardless of age/count |

A version is **retained** if it satisfies **either** criterion: within the top N versions (`MaxVersions`) **or** younger than `MaxAge`. The active version is never deleted when `KeepActiveAlways` is true. Cleanup runs automatically after each new version is activated (fire-and-forget, best-effort).

### RedisLiveConfigOptions

Section: `LiveConfig:Redis`

| Property | Type | Default | Description |
|---|---|---|---|
| `KeyPrefix` | `string` | `liveconfig` | Redis key prefix |
| `NotificationChannel` | `string` | `liveconfig:updated` | Pub/sub channel name |
| `Expiration` | `TimeSpan?` | `null` | TTL for Redis entries (null = no expiry) |

### LiveConfigHostOptions

Section: `LiveConfig:Hosting`

| Property | Type | Default | Description |
|---|---|---|---|
| `RedisNotificationChannel` | `string` | `liveconfig:updated` | Redis pub/sub channel |
| `ImportQueueName` | `string` | `liveconfig-import` | RabbitMQ import queue |
| `RollbackQueueName` | `string` | `liveconfig-rollback` | RabbitMQ rollback queue |
| `EnableMqConsumers` | `bool` | `true` | Register MassTransit consumers |
| `EnableRedisSubscriber` | `bool` | `true` | Start Redis subscriber |

### GSheetOptions

Section: `GSheet`

| Property | Type | Default | Description |
|---|---|---|---|
| `SpreadsheetId` | `string` | — | Google Sheets ID (required) |
| `ApplicationName` | `string` | `"LiveConfig"` | Google API app name |
| `FailOnImportErrors` | `bool` | `true` | Throw on import failures |
| `FailOnValidationErrors` | `bool` | `false` | Throw on validation errors |
| `Credentials.Method` | `enum` | `CredentialsBase64Env` | Credential resolution method |
| `Credentials.Value` | `string` | — | Credential value (depends on method) |

### JsonFileConfigSourceOptions

Section: `LiveConfig:Json`

| Property | Type | Default | Description |
|---|---|---|---|
| `Directory` | `string` | `./configs` | Config file directory |
| `Environment` | `string?` | `null` | Environment for overlay files |
| `SearchPattern` | `string` | `*.json` | File discovery pattern |
