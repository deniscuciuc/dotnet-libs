# DenisCuciuc.Platform.MongoDB

Unified MongoDB framework with repository pattern, migrations, seeding, time-series, CAS/actor pattern, and in-memory entity caching.

## Setup

```csharp
// Register (no startup dependency)
services.AddMongoDB(new MongoDBOptions
{
    ConnectionString = "mongodb://localhost:27017/mydb",
    MinConnectionPoolSize = 10,
    MaxConnectionPoolSize = 100,
    WaitQueueTimeout = TimeSpan.FromSeconds(30)
});

// Register startup pipeline (from DenisCuciuc.Platform.MongoDB.Startup)
services.AddPlatformMongoDBStartup();
```

### Startup (via `DenisCuciuc.Platform.MongoDB.Startup`)

`AddPlatformMongoDBStartup()` registers four `IStartup` tasks that run automatically via `RunWithStartupAsync()`:

| Order | Task | Description |
|---|---|---|
| 0 | `MongoConnectStartup` | Connects all `IMongoDBConnection` in parallel, marks guard |
| 1 | `MongoIndexStartup` | Applies repository indexes in parallel |
| 2 | `MongoMigrationStartup` | Runs pending migrations sequentially |
| 3 | `MongoSeedingStartup` | Runs registered seeders sequentially |

```csharp
var app = builder.Build();
await app.RunWithStartupAsync();  // all tasks run automatically in order
```

Optionally specify a migration target version:

```csharp
services.AddPlatformMongoDBStartup(targetVersion: new MigrationVersion(5));
```

If no `IMigrationRunner` or `ISeederRunner` is registered, those tasks are silently skipped.

---

## Repository Hierarchy

```
IRepository
    - Repository<TDocument>            - raw collection access (Primary + Secondary)
        - RepositoryWithIndex<T>         - auto-managed indexes
            - EntityRepository<T>          - ObjectId entities, builders, counters
                - ActorRepository<T, TActor> - CAS optimistic concurrency
```

---

## Entity Base Types

| Type | Purpose |
|---|---|
| `EntityBase` | `ObjectId Id`, `CreatedAt`, `ModifiedAt`, `[BsonIgnoreExtraElements]` |
| `EntityCas` | Extends `EntityBase` + `long Cas` (`[BsonElement("_cas")]`) for CAS |

```csharp
public class User : EntityBase
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
}

public class Wallet : EntityCas
{
    public decimal Balance { get; set; }
}
```

---

## EntityRepository\<T\>

Rich base for `EntityBase`-derived types. Register one concrete class per entity:

```csharp
public class UserRepository(IMongoDBProvider provider, IndexesBuilder<User> indexes, ILogger<UserRepository> logger)
    : EntityRepository<User>(provider, indexes, logger)
{
    public Task<User?> FindByEmailAsync(string email)
        => Primary.Find(Filter.Eq(x => x.Email, email)).FirstOrDefaultAsync();
}

services.AddRepository<IUserRepository, UserRepository>();
```

### Built-in capabilities

```csharp
// Builder shortcuts
Filter, Update, Projection, Sort, Index

// Read via secondary preferred replica
IFindFluent<T, T> Find(FilterDefinition<T> filter)
IFindFluent<BsonDocument, BsonDocument> Find(FilterDefinition<BsonDocument> filter)

// Atomic counter (FindOneAndUpdate upsert)
Task<long> IncrementAsync(string? suffix = null)

// Swallows DuplicateKey; returns false/null on conflict
Task<bool> ExecuteAndCheckDuplicateAsync(Func<Task> action)
Task<TDocument?> ExecuteAndCheckDuplicateAsync<TDocument>(Func<Task<TDocument>> action)
```

---

## Indexes

```csharp
public class UserRepository : EntityRepository<User>
{
    protected override void ConfigureIndexes(IndexesBuilder<User> builder)
    {
        builder.Add(new IndexDefinition<User>
        {
            Keys = Index.Ascending(x => x.Email),
            Options = new CreateIndexOptions { Unique = true, Name = "idx_email" }
        });
    }
}
```

Indexes are applied at startup via `RunMongoDBAsync()` using `IRepositoryApplyIndex`.

---

## Migrations

```csharp
[Migration(1)]
public class AddEmailIndex : Migration
{
    public override async Task Up(IMongoDatabase db)
    {
        var col = db.GetCollection<BsonDocument>("users");
        await col.Indexes.CreateOneAsync(
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("email"),
                new CreateIndexOptions { Unique = true }));
    }

    public override Task Down(IMongoDatabase db)
        => Task.CompletedTask;
}
```

Migrations run in version order; each version runs exactly once and is persisted.

---

## Seeding

```csharp
[Seeder("roles")]
public class RoleSeeder(IMongoDBProvider provider) : Seeder
{
    public override async Task SeedAsync()
    {
        var col = provider.Database.GetCollection<Role>("roles");
        var exists = await col.CountDocumentsAsync(FilterDefinition<Role>.Empty) > 0;
        if (!exists)
            await col.InsertManyAsync(DefaultRoles.All);
    }
}
```

---

## MongoDB Cache

Replace any `ICache<T>` binding with a MongoDB-backed implementation:

```csharp
services.AddMongoDBCache(); // registers ICache<T> -> MongoCache<T>
```

---

## CAS / Actor Pattern

Optimistic concurrency via Compare-And-Swap on the `_cas` field. Best for high-concurrency shared state (wallets, counters, game sessions).

### 1. Define the actor

```csharp
// Simple actor - full document replace on save
public class WalletActor(Wallet entity, ActorContext ctx, ILogger logger)
    : Actor<Wallet>(entity, ctx, logger)
{
    public void Deposit(decimal amount) => Entity.Balance += amount;
    public void Withdraw(decimal amount) => Entity.Balance -= amount;

    protected override void OnPreProcessing() { }
    protected override void OnPostProcessing() { }
}

// Observable actor - partial field-level updates
public class WalletActor(Wallet entity, ActorContext ctx, ILogger logger)
    : ObservableActor<Wallet>(entity, ctx, logger)
{
    public void Deposit(decimal amount)
    {
        Entity.Balance += amount;
        OnChanged(x => x.Balance); // only Balance field is sent to Mongo
    }

    protected override void OnPreProcessing() { }
    protected override void OnPostProcessing() { }
}
```

### 2. Define the factory

```csharp
public class WalletActorFactory(ILogger<WalletActor> logger)
    : IActorFactory<Wallet, WalletActor>
{
    public WalletActor Create(Wallet entity, ActorOwner? owner)
        => new(entity, new ActorContext(owner, entity.Id.ToString()), logger);
}
```

### 3. Define the repository

```csharp
public class WalletRepository(
    IServiceProvider sp,
    IMongoDBProvider provider,
    IndexesBuilder<Wallet> indexes,
    IEntityCache<Wallet> cache,
    IActorFactory<Wallet, WalletActor> factory,
    ILogger<WalletRepository> logger)
    : ActorRepository<Wallet, WalletActor>(sp, provider, indexes, cache, factory, null, logger)
{
}
```

### 4. Register

```csharp
services.AddActorRepository<WalletRepository, Wallet, WalletActor, IActorResult<Wallet>>()
    .WithFactory<WalletActorFactory>();
// WithCache<T>() is optional - MemoryEntityCache<T> is the default
```

### 5. Use

```csharp
public class PaymentService(IWalletRepository wallets)
{
    public Task<IActorResult<Wallet>> DepositAsync(ObjectId id, decimal amount)
        => wallets.Actor.ExecuteAsync(id, actor => actor.Deposit(amount));

    public Task<OperationResult<IActorResult<Wallet>, Wallet>> TryWithdrawAsync(
        ObjectId id, decimal amount)
        => wallets.Actor.ExecuteSafeAsync(id, actor => actor.Withdraw(amount));
}
```

`ExecuteSafeAsync` returns `OperationResult` - no exception thrown on `EntityNotFound` or `EntityDuplicated`.

### Entity cache

The default `MemoryEntityCache<T>` is scoped to the process (good for CAS retry windows of a few seconds). Swap to a distributed cache for multi-replica deployments:

```csharp
// Redis-backed example
public class RedisWalletCache(IRedisClient redis) : IEntityCache<Wallet> { ... }

services.AddActorRepository<...>()
    .WithCache<RedisWalletCache>();
```

### CAS retry policy

Built-in Polly policy retries up to **10 times** on `CasException` (stale version detected). Each retry re-fetches from DB (or cache) and re-applies the mutation.

---

## Utilities

### ObjectId <-> Guid

```csharp
ObjectId id = guid.ToObjectId();
Guid guid   = id.ToGuid();
```

### Atomic counter

```csharp
// Returns next value for the named sequence
long nextOrderId = await repository.IncrementAsync("order");
```

### DataSource enum

```csharp
DataSource.Primary   // ReadPreference.PrimaryPreferred
DataSource.Secondary // ReadPreference.SecondaryPreferred
```
