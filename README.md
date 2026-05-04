# DenisCuciuc.Platform

Modular .NET 10 platform libraries for configuration, startup orchestration,
data access, messaging, identity, storage, jobs, observability, and common web
service infrastructure.

This repository was migrated from the legacy internal platform and adapted to
the open-source Denis Cuciuc library stack. Package and namespace roots now use
`DenisCuciuc.Platform.*`, and the configuration schema uses raw section names
such as `MongoDB`, `Redis`, `Cqrs`, and `Jobs`.

## Installation

Use NuGet packages for stable consumption.

```bash
dotnet add package DenisCuciuc.Platform.Startup
dotnet add package DenisCuciuc.Platform.Configuration
```

For active development across multiple packages, consume this repository as a
git submodule and reference the specific projects you need.

```bash
git submodule add https://github.com/deniscuciuc/dotnet-platform libs/dotnet-platform
```

```xml
<ItemGroup>
  <ProjectReference Include="libs/dotnet-platform/src/DenisCuciuc.Platform.Startup/DenisCuciuc.Platform.Startup.csproj" />
  <ProjectReference Include="libs/dotnet-platform/src/DenisCuciuc.Platform.Configuration/DenisCuciuc.Platform.Configuration.csproj" />
</ItemGroup>
```

## Packages

| Package | Description |
| --- | --- |
| `DenisCuciuc.Platform.Configuration` | YAML config pipeline with convention-based typed options |
| `DenisCuciuc.Platform.Startup` | Ordered startup orchestration before `Host.RunAsync` |
| `DenisCuciuc.Platform.MongoDB` | MongoDB repository pattern, migrations, seeding, CAS/actor support |
| `DenisCuciuc.Platform.Postgres` | PostgreSQL and EF Core registration helpers |
| `DenisCuciuc.Platform.Redis` | Redis client, `ICache`, and composite caching |
| `DenisCuciuc.Platform.Cache` | Cache abstractions and shared caching primitives |
| `DenisCuciuc.Platform.MQ` | RabbitMQ integration through MassTransit |
| `DenisCuciuc.Platform.CQRS` | MediatR, validation, and ErrorOr request pipeline |
| `DenisCuciuc.Platform.Identity` | Multi-scheme token validation and identity helpers |
| `DenisCuciuc.Platform.Storage` | File storage facade with disk and S3 providers |
| `DenisCuciuc.Platform.ErrorHandling` | Exception middleware and `ErrorOr` to `IResult` mapping |
| `DenisCuciuc.Platform.HealthCheck` | Health endpoint helpers |
| `DenisCuciuc.Platform.Swagger` | Swagger UI and OpenAPI wiring |
| `DenisCuciuc.Platform.Swagger.Theme` | Styled Swagger theme integration |
| `DenisCuciuc.Platform.Serilog` | Structured logging and request logging helpers |
| `DenisCuciuc.Platform.Sentry` | Sentry and OpenTelemetry integration |
| `DenisCuciuc.Platform.Metrics` | Prometheus and OpenTelemetry metrics |
| `DenisCuciuc.Platform.Jobs` | Shared abstractions for background jobs |
| `DenisCuciuc.Platform.Domain` | Base domain types and domain-event primitives |

## Quick start

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
    cfg => cfg.AddConsumer<OrderCreatedConsumer>());
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

## Documentation

- [Getting started](docs/getting-started.md)
- [Configuration](docs/configuration.md)
- [Startup](docs/startup.md)
- [MongoDB](docs/mongodb.md)
- [Postgres](docs/postgres.md)
- [Redis](docs/redis.md)
- [Identity](docs/identity.md)
- [Messaging](docs/messaging.md)
- [Storage](docs/storage.md)
- [Observability](docs/observability.md)
- [Release process](docs/release-process.md)
- [Changelog](CHANGELOG.md)

## Examples

Working examples are available under `examples/`, including Web API, RabbitMQ,
worker, storage, and Hangfire and Quartz job setups backed by MongoDB or
Postgres.

## Validation

```bash
dotnet build --configuration Release
dotnet test --configuration Release
dotnet format --verify-no-changes --no-restore
```

## Release model

CI always restores, builds, tests, and format-checks the repo. Publishing is
manual through the `Release` GitHub Actions workflow.

- The workflow always builds and packs the repository.
- NuGet publishing is opt-in through the `publish_nuget` input.
- GitHub Release creation is opt-in through the `create_github_release` input.
- Release notes for the publish snapshot live in `CHANGELOG.md`.

## License

[PolyForm Strict](LICENSE)

Commercial use requires permission. Contact: <denis@deniscuciuc.dev>