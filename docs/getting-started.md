# Getting Started

## NuGet source

Published packages are intended for NuGet.org consumption.

```bash
dotnet nuget list source
```

If NuGet.org is not already configured locally, add it once:

```bash
dotnet nuget add source https://api.nuget.org/v3/index.json --name nuget.org
```

## Install packages

Pick only the packages your service needs.

```bash
# Core infrastructure
dotnet add package DenisCuciuc.Platform.Startup
dotnet add package DenisCuciuc.Platform.Configuration

# Data
dotnet add package DenisCuciuc.Platform.MongoDB
dotnet add package DenisCuciuc.Platform.Postgres
dotnet add package DenisCuciuc.Platform.Redis

# Observability
dotnet add package DenisCuciuc.Platform.Serilog
dotnet add package DenisCuciuc.Platform.Metrics
dotnet add package DenisCuciuc.Platform.Sentry

# Identity and messaging
dotnet add package DenisCuciuc.Platform.Identity
dotnet add package DenisCuciuc.Platform.MQ
dotnet add package DenisCuciuc.Platform.CQRS

# Web and storage
dotnet add package DenisCuciuc.Platform.ErrorHandling
dotnet add package DenisCuciuc.Platform.HealthCheck
dotnet add package DenisCuciuc.Platform.Swagger
dotnet add package DenisCuciuc.Platform.Storage
```

## Submodule workflow

For active multi-repo development, bring the repository in as source and add
project references to the packages you need.

```bash
git submodule add https://github.com/deniscuciuc/dotnet-platform libs/dotnet-platform
```

```xml
<ItemGroup>
  <ProjectReference Include="libs/dotnet-platform/src/DenisCuciuc.Platform.Configuration/DenisCuciuc.Platform.Configuration.csproj" />
  <ProjectReference Include="libs/dotnet-platform/src/DenisCuciuc.Platform.Startup/DenisCuciuc.Platform.Startup.csproj" />
</ItemGroup>
```

## Minimal setup example

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddPlatformConfiguration();
builder.Host.UsePlatformSerilog();
builder.AddPlatformSentry();

builder.Services.AddPlatformMetrics(builder.Configuration);
builder.Services.AddMongoDB(builder.Configuration);
builder.Services.AddPlatformRedis(builder.Configuration);
builder.Services.AddPlatformRabbitMq(
    builder.Configuration,
    cfg => cfg.AddConsumer<OrderConsumer>());
builder.Services.AddPlatformCqrsFromAssemblyOf<Program>(builder.Configuration);
builder.Services.AddPlatformIdentity(builder.Configuration);
builder.Services.AddPlatformFileStorage(builder.Configuration);

builder.Services.AddPlatformMongoDBStartup();
builder.Services.AddPlatformRedisStartup();
builder.Services.AddPlatformStorageStartup();

var app = builder.Build();

app.UsePlatformHealthChecks();
app.MapPlatformMetrics();

await app.RunWithStartupAsync();
```

## Configuration notes

The current package surface has been renamed to `DenisCuciuc.Platform.*`, but
default configuration sections now use raw names such as
`MongoDB`, `Redis`, `Cqrs`, and `Jobs`.

## Local packing

```bash
dotnet pack \
  --configuration Release \
  -p:PackageVersion=1.0.0 \
  -o .\artifacts
```

Push manually when needed:

```bash
dotnet nuget push .\artifacts\*.nupkg \
  --api-key YOUR_NUGET_API_KEY \
  --source https://api.nuget.org/v3/index.json \
  --skip-duplicate
```
