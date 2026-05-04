# DenisCuciuc.Platform.Redis

Redis client wrapper built on StackExchange.Redis with a clean `ICache` implementation, health checks, and startup lifecycle integration.

## Setup

```csharp
// Register the Redis client
services.AddPlatformRedis(configuration);

// Register ICache backed by Redis
services.AddPlatformRedisCache();

// Register startup pipeline (from DenisCuciuc.Platform.Redis.Startup)
services.AddPlatformRedisStartup();
```

### Startup (via `DenisCuciuc.Platform.Redis.Startup`)

`AddPlatformRedisStartup()` registers `RedisConnectStartup` (order 0) which connects all `IRedisConnection` instances in parallel and marks the `StartupGuard<IRedisClient>` as configured.

```csharp
var app = builder.Build();
await app.RunWithStartupAsync();  // RedisConnectStartup runs automatically
```

**appsettings.yml:**

```yaml
Redis:
    ConnectionString: localhost:6379
```

---

## Cache abstractions (`DenisCuciuc.Platform.Cache`)

`DenisCuciuc.Platform.Cache` owns all abstractions; `DenisCuciuc.Platform.Redis` provides the implementation.

```text
DenisCuciuc.Platform.Cache
 - ICache              - core get/set/delete
 - ICacheSerializer    - pluggable serialization
 - ICacheKeyBuilder    - composable key construction
 - CachePolicy         - expiry presets
 - CacheExtensions     - GetOrSet patterns
 - CompositeCache      - two-level memory + distributed
 - ICacheMetrics       - observability hook

DenisCuciuc.Platform.Redis
 - RedisCache : ICache - Redis-backed implementation (JSON serialiser)
```

### ICache

```csharp
public interface ICache
{
    Task<T?> GetAsync<T>(string key);
    Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null, When when = When.Always);
    Task<bool> DeleteAsync(string key);
}
```

---

## Key building

Use `ICacheKeyBuilder` to avoid raw string concatenation across the codebase:

```csharp
// DefaultCacheKeyBuilder uses ":" as separator
keyBuilder.Build("achievements", playerId)  // -> "achievements:12345"
keyBuilder.Build("leaderboard", "global")   // -> "leaderboard:global"
```

Register the default implementation:

```csharp
services.AddSingleton<ICacheKeyBuilder, DefaultCacheKeyBuilder>();
```

Or with a custom separator:

```csharp
services.AddSingleton<ICacheKeyBuilder>(_ => new DefaultCacheKeyBuilder("::"));
```

---

## GetOrSet pattern

The most common caching pattern - read through cache, populate on miss:

```csharp
var achievements = await cache.GetOrSetAsync(
    keyBuilder.Build("achievements", playerId),
    () => repository.LoadAchievementsAsync(playerId),
    TimeSpan.FromMinutes(10));
```

---

## Cache policies

Reusable expiry configurations:

```csharp
// Absolute expiry helpers
CachePolicy.Expire(TimeSpan.FromMinutes(5))
CachePolicy.Sliding(TimeSpan.FromMinutes(5))
CachePolicy.NoExpiry

// Usage
await cache.SetAsync(key, value, CachePolicy.Expire(TimeSpan.FromHours(1)));

await cache.GetOrSetAsync(key, factory, CachePolicy.Expire(TimeSpan.FromMinutes(30)));
```

---

## Two-level (composite) cache

Combine in-memory (fast) and Redis (distributed) into one `ICache`:

```csharp
var composite = new CompositeCache(
    primary:        memoryCache,           // fast layer
    secondary:      redisCache,            // distributed layer
    primaryExpiry:  TimeSpan.FromSeconds(30)); // backfill TTL

services.AddSingleton<ICache>(composite);
```

On a cache miss in memory, the value is fetched from Redis and written back to memory for subsequent reads.

---

## Serialization

The default `JsonCacheSerializer` (registered automatically by `AddRedisCache`) uses `System.Text.Json`.

Swap in a custom serializer (e.g. MessagePack) by implementing `ICacheSerializer`:

```csharp
services.AddSingleton<ICacheSerializer, MyMessagePackSerializer>();
services.AddRedisCache();
```

---

## Observability

Implement `ICacheMetrics` in your metrics layer and optionally inject it into a decorating `ICache` wrapper:

```csharp
public interface ICacheMetrics
{
    void RecordHit(string key);
    void RecordMiss(string key);
}
```

---

## Health check

```csharp
builder.Services.AddHealthChecks()
    .AddRedisHealthCheck();
```

Exposed at `/health` via `app.UseHealthChecks()`.

---

## Direct client access

For operations not covered by `ICache`, inject `IRedisClient` directly:

```csharp
public class MyService(IRedisClient redis)
{
    public Task<long> IncrementAsync(string key) =>
        redis.Database.StringIncrementAsync(key);
}
```
