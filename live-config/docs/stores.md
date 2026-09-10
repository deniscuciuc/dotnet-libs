# Config Stores

Config stores implement `IConfigStore` for persistent, versioned storage of configuration snapshots.

## Interface

```csharp
public interface IConfigStore
{
    Task<ConfigVersion?> GetActiveAsync(string configType, CancellationToken ct = default);
    Task<ConfigVersion?> GetVersionAsync(string configType, int version, CancellationToken ct = default);
    Task<IReadOnlyList<ConfigVersion>> GetRecentVersionsAsync(string configType, int count = 10, CancellationToken ct = default);
    Task<int> GetLatestVersionNumberAsync(string configType, CancellationToken ct = default);
    Task<string?> GetActiveHashAsync(string configType, CancellationToken ct = default);
    Task<int> CreateVersionAsync(string configType, string dataJson, string dataHash, string? createdBy = null, CancellationToken ct = default);
    Task<bool> ActivateVersionAsync(string configType, int version, CancellationToken ct = default);
    Task<IReadOnlyDictionary<string, int>> GetManifestAsync(CancellationToken ct = default);
    Task<int> CleanupOldVersionsAsync(string configType, ConfigRetentionOptions retention, CancellationToken ct = default);
}
```

## MongoDB Store (`CoreLibs.LiveConfig.Store.MongoDB`)

### Setup

```csharp
builder.Services.AddLiveConfigMongoStore(builder.Configuration);
```

### Configuration

```yaml
liveconfig:
  store:
    mongodb:
      collectionName: config_snapshots
  retention:
    enabled: true
    maxVersions: 30
    maxAge: "7.00:00:00"
    keepActiveAlways: true
```

| Option | Default | Description |
|---|---|---|
| `CollectionName` | `config_snapshots` | MongoDB collection name |

Version retention is configured globally via `ConfigRetentionOptions` — see [configuration.md](configuration.md#configretentionoptions).

### Features

- **Optimistic concurrency** — version numbers auto-increment using `FindOneAndUpdate` with `$max`
- **Atomic activation** — deactivates old + activates new in a single bulk-write operation
- **Auto-indexing** — creates compound indexes on `(ConfigType, Version)` and `(ConfigType, IsActive)` at startup
- **Manifest query** — aggregation pipeline across all active documents

### Entity

```csharp
public sealed class MongoConfigSnapshotEntity
{
    public ObjectId Id { get; set; }
    public required string ConfigType { get; set; }
    public int Version { get; set; }
    public required string DataJson { get; set; }
    public required string DataHash { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
}
```

---

## Postgres Store (`CoreLibs.LiveConfig.Store.Postgres`)

### Setup

```csharp
builder.Services.AddLiveConfigPostgresStore(builder.Configuration);
```

Requires `AddCorePostgres<LiveConfigDbContext>()` from the platform for the database connection.

### Configuration

```yaml
liveconfig:
  store:
    postgres:
      schema: public
      tableName: config_snapshots
  retention:
    enabled: true
    maxVersions: 30
    maxAge: "7.00:00:00"
    keepActiveAlways: true
```

| Option | Default | Description |
|---|---|---|
| `Schema` | `public` | Database schema |
| `TableName` | `config_snapshots` | Table name |

Version retention is configured globally via `ConfigRetentionOptions` — see [configuration.md](configuration.md#configretentionoptions).

### Features

- **EF Core integration** — uses `LiveConfigDbContext` with standard migrations
- **Batch activation** — `ExecuteUpdateAsync` for efficient bulk operations
- **Retry on conflict** — automatic retry with version re-read on concurrency exceptions

### Migration

```bash
dotnet ef migrations add InitialLiveConfig -c LiveConfigDbContext
dotnet ef database update -c LiveConfigDbContext
```

---

## Custom Store

Implement `IConfigStore` and register:

```csharp
public class DynamoDbConfigStore : IConfigStore
{
    // Implement all 8 methods...
}

builder.Services.AddSingleton<IConfigStore, DynamoDbConfigStore>();
```
