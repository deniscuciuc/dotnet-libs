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
  <!-- Core framework (always required) -->
  <ProjectReference Include="libs/dotnet-libs/live-config/src/CoreLibs.LiveConfig.Startup/CoreLibs.LiveConfig.Startup.csproj" />

  <!-- Config sources: pick one or more -->
  <ProjectReference Include="libs/dotnet-libs/live-config/src/CoreLibs.LiveConfig.Json/CoreLibs.LiveConfig.Json.csproj" />
  <ProjectReference Include="libs/dotnet-libs/live-config/src/CoreLibs.LiveConfig.GSheet/CoreLibs.LiveConfig.GSheet.csproj" />

  <!-- Config store: pick one -->
  <ProjectReference Include="libs/dotnet-libs/live-config/src/CoreLibs.LiveConfig.Store.MongoDB/CoreLibs.LiveConfig.Store.MongoDB.csproj" />

  <!-- Distribution, for multi-instance deployments -->
  <ProjectReference Include="libs/dotnet-libs/live-config/src/CoreLibs.LiveConfig.Redis/CoreLibs.LiveConfig.Redis.csproj" />
</ItemGroup>
```

## Minimal Setup — Config Manager Service

A config manager service fetches configs from a source (e.g. Google Sheets), stores versioned snapshots, and distributes them:

```csharp
var builder = WebApplication.CreateBuilder(args);

// 1. Core framework
builder.Services.AddLiveConfig(builder.Configuration);

// 2. Source — Google Sheets
builder.Services.AddLiveConfigGSheet(builder.Configuration);
builder.Services.AddGSheetImportersInAssemblyOf<Program>();

// 3. Store — MongoDB
builder.Services.AddLiveConfigMongoStore(builder.Configuration);

// 4. Distributor — Redis
builder.Services.AddLiveConfigRedis(builder.Configuration);

// 5. Hosting — MQ consumers + Redis subscriber
builder.Services.AddLiveConfigHosting(builder.Configuration);
builder.Services.AddMassTransit(x =>
{
    x.AddLiveConfigConsumers();
    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMq:Host"]);
        ctx.ConfigureLiveConfigEndpoints(cfg);
    });
});

// 6. Preload critical configs before traffic
builder.Services.AddLiveConfigPreloadService();

var app = builder.Build();
await app.RunAsync();
```

## Minimal Setup — Consumer Service

A consumer service reads live configs and receives updates:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLiveConfig(builder.Configuration);
builder.Services.AddLiveConfigCachedConfig<BonusConfig>("bonuses");
builder.Services.AddLiveConfigRedis(builder.Configuration);
builder.Services.AddLiveConfigPreloadService();
builder.Services.AddLiveConfigHosting(builder.Configuration);

var app = builder.Build();

app.MapGet("/bonuses", (IConfigCache<IReadOnlyList<BonusConfig>> cache) =>
    Results.Ok(cache.Current));

await app.RunAsync();
```

## Configuration

All options use the `LiveConfig` prefix and support both YAML and JSON:

```yaml
liveconfig:
  criticalConfigTypes:
    - bonuses
  store:
    mongodb:
      collectionName: config_snapshots
  redis:
    keyPrefix: liveconfig
  hosting:
    enableMqConsumers: true
    enableRedisSubscriber: true

GSheet:
  spreadsheetId: "YOUR_SPREADSHEET_ID"
  credentials:
    method: CredentialsBase64Env
    value: GOOGLE_SHEET_CREDENTIALS
```

See [Configuration Reference](configuration.md) for all available options.
