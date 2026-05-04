# DenisCuciuc.Platform.Postgres

PostgreSQL integration via Entity Framework Core with domain event dispatch and startup migrations.

## Packages

| Package | Purpose |
|---|---|
| `DenisCuciuc.Platform.Postgres` | `DbContext` registration, domain event interceptor |
| `DenisCuciuc.Platform.Postgres.Startup` | Startup pipeline: applies migrations automatically |

---

## Setup

```csharp
// Register (from DenisCuciuc.Platform.Postgres)
services.AddPlatformPostgres<AppDbContext>(configuration);

// Register startup pipeline (from DenisCuciuc.Platform.Postgres.Startup)
services.AddPlatformPostgresStartup<AppDbContext>();
```

```csharp
var app = builder.Build();
await app.RunWithStartupAsync();  // applies migrations then starts the app
```

---

## Configuration

Section path: `Postgres` (default).

```yml
Postgres:
    connectionString: "Host=localhost;Port=5432;Database=mydb;Username=user;Password=pass"
    maxRetryCount: 3        # 0-10, default 3
    commandTimeout: 30      # seconds, 1-300, default 30
    enableSensitiveDataLogging: false
    enableDetailedErrors: false
    namingConvention: SnakeCase   # None | SnakeCase | LowerCase | CamelCase
```

| Property | Type | Default | Description |
|---|---|---|---|
| `connectionString` | string | _(required)_ | Npgsql connection string |
| `maxRetryCount` | int | `3` | Transient failure retry count |
| `commandTimeout` | int | `30` | EF command timeout in seconds |
| `enableSensitiveDataLogging` | bool | `false` | Log parameter values (dev only) |
| `enableDetailedErrors` | bool | `false` | Detailed EF error messages (dev only) |
| `enableDynamicJson` | bool | `false` | Enables dynamic JSON serialization for open-ended types (e.g. `Dictionary<string,string>`) stored in JSONB/JSON columns |
| `namingConvention` | enum | `SnakeCase` | EF Core naming convention: `None`, `SnakeCase`, `LowerCase`, `CamelCase` |

### Register from config

```csharp
services.AddPlatformPostgres<AppDbContext>(configuration);
// uses section "Postgres" by default

services.AddPlatformPostgres<AppDbContext>(configuration, sectionPath: "database");
```

### Register with inline options

```csharp
services.AddPlatformPostgres<AppDbContext>(options =>
{
    options.ConnectionString = "Host=localhost;Database=mydb;Username=user;Password=pass";
    options.MaxRetryCount = 5;
    options.NamingConvention = PostgresNamingConvention.SnakeCase; // default
});
```

---

## Naming Conventions

EF Core naming conventions are applied via the [`EFCore.NamingConventions`](https://github.com/efcore/EFCore.NamingConventions) package. The convention controls how entity and property names are translated to database table/column names.

| Value | Example | Notes |
|---|---|---|
| `None` | `DriveItem` -> `DriveItem` | EF Core defaults, no transformation |
| `SnakeCase` _(default)_ | `DriveItem` -> `drive_item` | Recommended for PostgreSQL |
| `LowerCase` | `DriveItem` -> `driveitem` | All lowercase, no separator |
| `CamelCase` | `DriveItem` -> `driveItem` | camelCase |

Configure via YAML:

```yml
Postgres:
    namingConvention: SnakeCase
```

Or inline:

```csharp
services.AddPlatformPostgres<AppDbContext>(options =>
{
    options.ConnectionString = "...";
    options.NamingConvention = PostgresNamingConvention.None; // disable if using Fluent API names directly
});
```

### Custom DbContext configuration

```csharp
services.AddPlatformPostgres<AppDbContext>(configuration, configureDbContext: dbOptions =>
{
    dbOptions.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
});
```

---

## JSON / JSONB Support

By default Npgsql 8+ requires an explicit opt-in for dynamic (open-ended) JSON types. Enable it via configuration or code.

### Enable via configuration (recommended)

```yml
Postgres:
    connectionString: "..."
    enableDynamicJson: true
```

This calls `NpgsqlDataSourceBuilder.EnableDynamicJson()` so that types such as `Dictionary<string, string>`, `object`, and other open-ended types can be round-tripped through JSONB/JSON columns.

### Enable via inline options

```csharp
services.AddPlatformPostgres<AppDbContext>(options =>
{
    options.ConnectionString = "...";
    options.EnableDynamicJson = true;
});
```

### Custom data source configuration

For advanced scenarios - custom `JsonSerializerOptions`, Newtonsoft.Json (`UseJsonNet()`), or enum type mappings - pass a `configureDataSource` callback. When a callback is supplied (or `EnableDynamicJson` is `true`), an `NpgsqlDataSource` is built and passed to EF Core instead of a raw connection string.

```csharp
services.AddPlatformPostgres<AppDbContext>(configuration, configureDataSource: builder =>
{
    builder.EnableDynamicJson();
    // or: builder.UseJsonNet();
    // or: builder.ConfigureJsonOptions(opts => opts.PropertyNamingPolicy = null);
});
```

---

## Domain Events

When `AddPlatformDomainEvents()` is registered, `DomainEventSaveChangesInterceptor` is automatically added to the `DbContext`. After each successful `SaveChangesAsync`, domain events accumulated on `Entity` instances are dispatched via `IDomainEventDispatcher`.

```csharp
// Register domain events (from DenisCuciuc.Platform.Domain)
services.AddPlatformDomainEvents();

// Register Postgres - interceptor is wired automatically
services.AddPlatformPostgres<AppDbContext>(configuration);
```

Events are only dispatched if `IDomainEventDispatcher` is available in the service container. If it is not registered, the interceptor is not added.

---

## Startup Pipeline

`AddPlatformPostgresStartup<TContext>()` registers:

| Order | Task | Description |
|---|---|---|
| 0 | `PostgresMigrateStartup<TContext>` | Calls `MigrateAsync()` to apply pending EF migrations |

A `StartupGuard<TContext>` is also registered, blocking any service that depends on the database being ready until migrations complete.

```csharp
services.AddPlatformPostgresStartup<AppDbContext>();

var app = builder.Build();
await app.RunWithStartupAsync();
```

---

## DbContext Example

```csharp
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Note> Notes => Set<Note>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
```

---

## Migrations

Create and apply migrations as normal with `dotnet ef`:

```bash
dotnet ef migrations add InitialCreate --project src/MyApi
dotnet ef database update --project src/MyApi
```

At runtime, `AddPlatformPostgresStartup<AppDbContext>` applies all pending migrations automatically before the application starts serving requests.
