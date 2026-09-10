# Getting Started

## Referencing the projects

This repository is not published to NuGet. Add it as a submodule, then reference the
projects you need by relative path.

```bash
git submodule add https://github.com/deniscuciuc/dotnet-libs libs/dotnet-libs
git submodule update --init --recursive
```

```xml
<ItemGroup>
  <!-- Core infrastructure -->
  <ProjectReference Include="libs/dotnet-libs/platform/src/CoreLibs.Configuration/CoreLibs.Configuration.csproj" />
  <ProjectReference Include="libs/dotnet-libs/platform/src/CoreLibs.Startup/CoreLibs.Startup.csproj" />
  <ProjectReference Include="libs/dotnet-libs/platform/src/CoreLibs.Domain/CoreLibs.Domain.csproj" />

  <!-- Data -->
  <ProjectReference Include="libs/dotnet-libs/platform/src/CoreLibs.MongoDB.Startup/CoreLibs.MongoDB.Startup.csproj" />
  <ProjectReference Include="libs/dotnet-libs/platform/src/CoreLibs.Redis.Startup/CoreLibs.Redis.Startup.csproj" />
</ItemGroup>
```

Reference the `*.Startup` project rather than the library it wraps when you want the
component registered into the startup orchestrator — it references the library itself.

Pin the submodule to a tag for reproducible builds:

```bash
git -C libs/dotnet-libs checkout v2.0.0
```

## Minimal Setup Example

```csharp
var builder = WebApplication.CreateBuilder(args);

// YAML config
builder.AddCoreConfiguration();

// Serilog
builder.Host.UseCoreSerilog();

// Sentry
builder.AddCoreSentry();

// Metrics
builder.Services.AddCoreMetrics(builder.Configuration);

// MongoDB
builder.Services.AddMongoDB(builder.Configuration);
builder.Services.AddRepository<IUserRepository, UserRepository>();

// Redis
builder.Services.AddCoreRedis(builder.Configuration);

// Storage
builder.Services.AddCoreFileStorage(builder.Configuration);

// Messaging
builder.Services.AddCoreRabbitMq(
  builder.Configuration,
  cfg => cfg.AddConsumer<OrderConsumer>());

// Health checks
builder.Services.AddHealthChecks()
    .AddMongoDBHealthCheck()
    .AddRedisHealthCheck();

// CQRS
builder.Services.AddCoreCqrsFromAssemblyOf<Program>(builder.Configuration);

// Startup integrations
builder.Services.AddCoreMongoDBStartup();
builder.Services.AddCoreRedisStartup();
builder.Services.AddCoreStorageStartup();

var app = builder.Build();

app.UseHealthChecks();    // /health
app.UseErrorHandling();   // global exception middleware
app.MapCoreMetrics();       // /metrics

await app.RunWithStartupAsync();
```
