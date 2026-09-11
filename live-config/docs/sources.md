# Config Sources

Config sources implement `IConfigSource` to fetch configuration data from external systems.

## Interface

```csharp
public interface IConfigSource
{
    string SourceName { get; }
    Task<ConfigSnapshot> FetchAsync(string configType, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<string, ConfigSnapshot>> FetchAllAsync(CancellationToken cancellationToken = default);
}
```

## Built-in Sources

### Google Sheets (`CoreLibs.LiveConfig.GSheet`)

Full-featured source for managing configs in Google Sheets with CSV parsing, validation, and dependency-ordered imports. See [Google Sheets Integration](gsheet.md) for details.

```csharp
builder.Services.AddLiveConfigGSheet(builder.Configuration);
builder.Services.AddGSheetImportersInAssemblyOf<Program>();
```

### JSON Files (`CoreLibs.LiveConfig.Json`)

Directory-based source with environment layering. Reads `{type}.json` and optionally overlays `{type}.{env}.json`:

```csharp
builder.Services.AddLiveConfigJsonSource(builder.Configuration);
// or
builder.Services.AddLiveConfigJsonSource(opts =>
{
    opts.Directory = "./configs";
    opts.Environment = "Production";
});
```

**File structure:**
```
configs/
├── bonuses.json              # Base config
├── bonuses.Production.json   # Production overlay
├── game-settings.json
└── translations.json
```

**Configuration:**

```yaml
liveconfig:
  json:
    directory: "./configs"
    environment: "Production"
    searchPattern: "*.json"
```

| Option | Default | Description |
|---|---|---|
| `Directory` | `./configs` | Path to config file directory |
| `Environment` | `null` | Environment name for layering |
| `SearchPattern` | `*.json` | Glob pattern for discovery |

## Custom Sources

Implement `IConfigSource` and register:

```csharp
[ConfigSource("my-api")]
public class ApiConfigSource(HttpClient http) : IConfigSource
{
    public string SourceName => "my-api";

    public async Task<ConfigSnapshot> FetchAsync(string configType, CancellationToken ct = default)
    {
        var json = await http.GetStringAsync($"/configs/{configType}", ct);
        var hash = ConfigHasher.ComputeHash(json);
        return new ConfigSnapshot(configType, json, hash, DateTimeOffset.UtcNow);
    }

    public async Task<IReadOnlyDictionary<string, ConfigSnapshot>> FetchAllAsync(CancellationToken ct = default)
    {
        // Fetch all available configs
        var manifest = await http.GetFromJsonAsync<string[]>("/configs", ct);
        var snapshots = new Dictionary<string, ConfigSnapshot>();

        foreach (var type in manifest ?? [])
        {
            snapshots[type] = await FetchAsync(type, ct);
        }

        return snapshots;
    }
}

// Register
builder.Services.AddLiveConfigSource<ApiConfigSource>();
```

## Source Registration

Sources are auto-discovered by assembly scanning or registered explicitly:

```csharp
// Explicit
builder.Services.AddLiveConfigSource<MySource>();

// Assembly scanning
builder.Services.AddLiveConfigSourcesFromAssemblyOf<Program>();
```

All registered `IConfigSource` implementations are collected into the `ConfigSourceRegistry` at startup.
