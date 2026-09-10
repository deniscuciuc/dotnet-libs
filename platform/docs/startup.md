# CoreLibs.Startup

Ordered startup orchestration that ensures infrastructure dependencies (DB connections, index creation, migrations, cache warm-up) are ready before the application starts serving requests. Supports priority ordering, environment-aware tasks, attribute-based auto-registration, and structured logging of every phase.

## Concept

```text
Host.Build()
  │
  ▼
RunWithStartupAsync()
  │
  ├── Optional pre-startup hook  (edge cases only)
  │
  ├── foreach IStartup (ordered by Order) → StartupAsync()
  │     ├── MongoConnectStartup       (0)  — connect + MarkConfigured
  │     ├── MongoIndexStartup         (1)  — apply indexes
  │     ├── MongoMigrationStartup     (2)  — sequential migrations
  │     ├── MongoSeedingStartup       (3)  — sequential seeders
  │     ├── RedisConnectStartup       (0)  — connect + MarkConfigured
    │     ├── StorageInitializationStartup (0) — run storage initializers + MarkConfigured
  │     ├── custom tasks              (10+)
  │     ├── skip if [StartupTask(Environments)] doesn't match
  │     └── log start / completion / failure
  │
  ├── StartupGuard<IMongoDBProvider>  (int.MaxValue) — fail if not configured
  ├── StartupGuard<IRedisClient>      (int.MaxValue) — fail if not configured
    ├── StartupGuard<IFileStorage>      (int.MaxValue) — fail if not configured
  │
  └── Host.RunAsync()  ← start serving
```

---

## Usage

Infrastructure startup is fully driven by the `IStartup` pipeline. The integration packages register dedicated startup tasks — no manual orchestration needed.

| Package | Provides |
| --- | --- |
| `CoreLibs.MongoDB.Startup` | `MongoConnectStartup`, `MongoIndexStartup`, `MongoMigrationStartup`, `MongoSeedingStartup` |
| `CoreLibs.Redis.Startup` | `RedisConnectStartup` |
| `CoreLibs.Storage.Startup` | `StorageInitializationStartup` |

### DI registration

```csharp
// Core registrations (no startup dependency)
services.AddMongoDB(configuration);
services.AddCoreRedis(configuration);
services.AddCoreFileStorage(configuration);

// Register startup pipeline (from the .Startup packages)
services.AddCoreMongoDBStartup();   // using CoreLibs.MongoDB.Startup
services.AddCoreRedisStartup();     // using CoreLibs.Redis.Startup
services.AddCoreStorageStartup();   // using CoreLibs.Storage.Startup
```

`AddCoreMongoDBStartup()` optionally accepts a target migration version:

```csharp
services.AddCoreMongoDBStartup(targetVersion: new MigrationVersion(5));
```

### Run

```csharp
var app = builder.Build();

await app.RunWithStartupAsync();
// All IStartup tasks run automatically in order:
//   MongoConnectStartup (0) → MongoIndexStartup (1) → MongoMigrationStartup (2) →
//   MongoSeedingStartup (3) → RedisConnectStartup (0) → StorageInitializationStartup (0) → your tasks → guards
```

The pre-startup hook is still available for edge cases:

```csharp
await app.RunWithStartupAsync(async startup =>
{
    // Rare: manual work before the IStartup pipeline runs
});
```

---

## IHostStartup

The context object passed to every startup task and the pre-startup hook:

```csharp
public interface IHostStartup
{
    IServiceProvider   Services      { get; }
    IConfiguration     Configuration { get; }
    IHostEnvironment   Environment   { get; }
    ILogger            Logger        { get; }
}
```

```csharp
startup.MarkConfigured<T>()   // marks StartupGuard<T> as configured
startup.Services               // IServiceProvider
startup.Configuration          // IConfiguration
startup.Environment            // IHostEnvironment
startup.Logger                 // ILogger (category: "CoreLibs.Startup")
```

---

## Startup guards

Guards ensure a component was explicitly configured during startup. If not, the host throws at boot with a clear message. Guards always run last (`Order = int.MaxValue`).

The integration packages register guards automatically — `MongoConnectStartup` and `RedisConnectStartup` call `MarkConfigured<T>()` after a successful connection, so the guard passes.

`StorageInitializationStartup` also calls `MarkConfigured<IFileStorage>()` after resolving storage and running all `IStorageInitializer` implementations.

```csharp
// Registered automatically by AddCoreMongoDBStartup() / AddCoreRedisStartup()
// Manual registration for your own services:
services.AddCoreStartupGuard<IMyService>();
```

---

## Custom startup tasks

### Manual registration

```csharp
public class WarmUpCacheTask : IStartup
{
    public int Order => 10;

    public async Task StartupAsync(IHostStartup host, CancellationToken ct)
    {
        host.Logger.LogInformation("Warming up cache...");
        var cache = host.Services.GetRequiredService<ICache>();
        await cache.SetAsync("health", "ok", TimeSpan.FromMinutes(5));
    }
}

services.AddCoreStartup<WarmUpCacheTask>();
```

### Attribute-based auto-registration

Decorate classes with `[StartupTask]` and scan an assembly:

```csharp
[StartupTask(Order = 20, Environments = ["Production", "Staging"])]
public class SeedProductionData : IStartup
{
    public async Task StartupAsync(IHostStartup host, CancellationToken ct)
    {
        // Only runs in Production and Staging
        await SeedAsync(host.Services, ct);
    }
}

// Program.cs — registers all [StartupTask] classes in the assembly
services.AddCoreStartupTasksFromAssemblyOf<SeedProductionData>();
```

| `[StartupTask]` property | Type | Default | Description |
| --- | --- | --- | --- |
| `Order` | `int` | `0` | Execution priority (lower = first) |
| `Environments` | `string[]` | `[]` (all) | Restrict to listed environments only |

---

## Execution order

All registered `IStartup` tasks are sorted by `Order` (ascending) before execution:

| Order | Task | Package |
| --- | --- | --- |
| 0 | `MongoConnectStartup` | `CoreLibs.MongoDB.Startup` |
| 0 | `RedisConnectStartup` | `CoreLibs.Redis.Startup` |
| 0 | `StorageInitializationStartup` | `CoreLibs.Storage.Startup` |
| 1 | `MongoIndexStartup` | `CoreLibs.MongoDB.Startup` |
| 2 | `MongoMigrationStartup` | `CoreLibs.MongoDB.Startup` |
| 3 | `MongoSeedingStartup` | `CoreLibs.MongoDB.Startup` |
| 10+ | Custom tasks | Your app |
| `int.MaxValue` | `StartupGuard<T>` | `CoreLibs.Startup` |

Tasks with the same order run in registration order.

---

## Logging

Every startup task is logged automatically:

```text
info: CoreLibs.Startup[0] Running startup task WarmUpCacheTask (order 10) ...
info: CoreLibs.Startup[0] Completed startup task WarmUpCacheTask
info: CoreLibs.Startup[0] Running startup task StartupGuard`1 (order 2147483647) ...
info: CoreLibs.Startup[0] Completed startup task StartupGuard`1
```

Environment-skipped tasks are logged at `Debug` level.

---

## DI extensions reference

### Core Package

| Method | Description |
| --- | --- |
| `AddCoreStartupGuard<T>()` | Registers a `StartupGuard<T>` that fails if not configured |
| `AddCoreStartup<T>()` | Registers any `IStartup` implementation |
| `AddCoreStartupTasksFromAssemblyOf<T>()` | Scans assembly for `[StartupTask]` classes |
| `RunWithStartupAsync(host, preStartup?, ct)` | Orchestrates pre-startup → ordered tasks → host run |
| `MarkConfigured<T>()` | Marks a `StartupGuard<T>` as configured |

### CoreLibs.MongoDB.Startup

| Method | Description |
| --- | --- |
| `AddCoreMongoDBStartup(version?)` | Registers all MongoDB startup tasks + guard |
| `MarkMongoDBConfigured()` | Manual guard mark (rarely needed) |

### CoreLibs.Redis.Startup

| Method | Description |
| --- | --- |
| `AddCoreRedisStartup()` | Registers `RedisConnectStartup` + guard |
| `MarkRedisConfigured()` | Manual guard mark (rarely needed) |

### CoreLibs.Storage.Startup

| Method | Description |
| --- | --- |
| `AddCoreStorageStartup()` | Registers `StorageInitializationStartup` + guard |

---

## Migration from old API

| Old | New | Notes |
| --- | --- | --- |
| `AddStartupChecker<T>()` | `AddCoreStartupGuard<T>()` | Removed |
| `StartupAndRunAsync(startup)` | `RunWithStartupAsync(preStartup?)` | Removed |
| `StartupChecker<T>` | `StartupGuard<T>` | Removed |
| `Configure<T>()` | `MarkConfigured<T>()` | Removed |
| `IStartup.StartupAsync(ct)` | `IStartup.StartupAsync(host, ct)` | Now receives `IHostStartup` |
| Manual `RunMongoDBAsync()` hook | `AddCoreMongoDBStartup()` | Fully `IStartup`-driven |
| Manual `RunRedisAsync()` hook | `AddCoreRedisStartup()` | Fully `IStartup`-driven |
