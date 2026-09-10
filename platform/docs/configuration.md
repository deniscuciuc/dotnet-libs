# CoreLibs.Configuration

Opinionated, composable YAML configuration pipeline for .NET services. Provides a single entry point that wires up a layered file strategy, environment variable loading, typed options binding with validation, and safe diagnostics helpers.

## Setup

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.AddCoreConfiguration();
```

Default layers loaded (in order of ascending precedence):

| Layer | File | Optional |
| --- | --- | --- |
| Base | `appsettings.yml` | ✓ |
| Environment-specific | `appsettings.{ASPNETCORE_ENVIRONMENT}.yml` | ✓ |
| Environment variables | unprefixed (`Section__Key`) | — |

All file sources reload automatically on change.

---

## Options

Pass a configure action to adjust behaviour:

```csharp
builder.AddCoreConfiguration(options =>
{
    options.BaseFileName = "appsettings";   // default
    options.Extension    = "yml";           // default
    options.IncludeLocal = true;            // adds appsettings.Local.yml
    options.EnvironmentVariablesPrefix = null;    // default; optional custom prefix
});
```

Full options reference:

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `BaseFileName` | `string` | `"appsettings"` | Base file name without extension |
| `Extension` | `string` | `"yml"` | File extension |
| `Profile` | `ConfigurationProfile` | `Default` | Preset strategy (see below) |
| `IncludeEnvironment` | `bool` | `true` | Add `appsettings.{Env}.yml` |
| `IncludeLocal` | `bool` | `false` | Add `appsettings.Local.yml` |
| `IncludeEnvironmentVariables` | `bool` | `true` | Add environment variables source |
| `EnvironmentVariablesPrefix` | `string?` | `null` | Optional prefix filter for env vars; `null` loads all |
| `Optional` | `bool` | `true` | Whether the base file is optional |
| `ReloadOnChange` | `bool` | `true` | Reload when files change |

---

## Profiles

Profiles are presets that drive the file-inclusion strategy. They override the individual `Include*` flags, so they are most useful when you want zero-config setup per deployment target.

```csharp
builder.AddCoreConfiguration(options =>
{
    options.Profile = ConfigurationProfile.Cloud;
});
```

| Profile | Base | Env-specific | Local | Env vars |
| --- | --- | --- | --- | --- |
| `Default` | ✓ | ✓ | per flag | ✓ |
| `Minimal` | ✓ | ✗ | ✗ | ✓ |
| `Cloud` | ✓ | ✓ | ✗ | ✓ |
| `LocalDev` | ✓ | ✓ | ✓ | ✓ |

> `Default` respects all individual flags. The other profiles override them.

---

## Typed options binding with validation

`AddCoreOptions<T>` is the primary way to bind configuration sections to strongly-typed classes.
It wires up `DataAnnotations` validation and calls `ValidateOnStart()` so misconfigured apps fail at
boot, not at runtime. The registered type is available as both `IOptions<T>` and
`IOptionsMonitor<T>` (live-reload on file change).

### Convention-based section naming

When no explicit section name is provided, the section is derived from the type name:

| Type | Resolved section |
| --- | --- |
| `MongoOptions` | `Mongo` |
| `RedisOptions` | `Redis` |
| `CoreSentryOptions` | `Sentry` |
| `MongoDBOptions` | `MongoDB` |

Rule: strip trailing `Options` and strip leading `Core`.

### Usage

```csharp
// Program.cs — convention-based (recommended)
services.AddCoreOptions<MongoOptions>(builder.Configuration);
services.AddCoreOptions<RedisOptions>(builder.Configuration);

// Explicit section override
services.AddCoreOptions<MongoOptions>(builder.Configuration, sectionName: "infrastructure:mongo");
```

```csharp
// MongoOptions.cs
public sealed class MongoOptions
{
    [Required]
    public string ConnectionString { get; set; } = string.Empty;

    [Range(1, 100)]
    public int MaxConnectionPoolSize { get; set; } = 50;
}
```

```csharp
// Inject as IOptions<T> (snapshot) or IOptionsMonitor<T> (live-reload)
public class MongoRepository(IOptionsMonitor<MongoOptions> options)
{
    public string GetConnectionString() => options.CurrentValue.ConnectionString;
}
```

**appsettings.yml** matching the convention:

```yaml
Mongo:
    ConnectionString: mongodb://localhost:27017/mydb
    MaxConnectionPoolSize: 50
Redis:
    Host: localhost
    Port: 6379
```

---

## Example config

**appsettings.yml:**

```yaml
Mongo:
    ConnectionString: mongodb://localhost:27017/mydb
    MinConnectionPoolSize: 5
    MaxConnectionPoolSize: 50
Redis:
    Host: localhost
    Port: 6379
Sentry:
    Dsn: ""
    Environment: development

Serilog:
  MinimumLevel:
    Default: Information
```

**appsettings.Production.yml:**

```yaml
Mongo:
    ConnectionString: ${MONGO_CONNECTION_STRING}
Redis:
    Host: ${REDIS_HOST}
    Password: ${REDIS_PASSWORD}
Sentry:
    Dsn: ${SENTRY_DSN}
    Environment: production
```

**appsettings.Local.yml** _(git-ignored, developer overrides)_:

```yaml
Mongo:
    ConnectionString: mongodb://localhost:27017/local-dev
```

---

## Diagnostics

Print active configuration values with sensitive keys masked:

```csharp
// Masks keys containing: Password, Secret, Token, Key, ConnectionString
app.Configuration.PrintConfiguration();

// Filter to a specific prefix only
app.Configuration.PrintConfiguration(key => key.StartsWith("Mongo"));
```

Print the ordered list of active configuration providers (useful for debugging layering issues):

```csharp
app.Configuration.PrintSources();
```

---

## Notes

- Powered by [NetEscapades.Configuration.Yaml](https://github.com/andrewlock/NetEscapades.Configuration.Yaml)
- YAML and JSON config sources are merged — YAML takes precedence over JSON for the same key
- Environment variables override all file sources (standard .NET config precedence)
- Use unprefixed env variables (`Section__Key`) by default; set a custom prefix only if you explicitly need it
