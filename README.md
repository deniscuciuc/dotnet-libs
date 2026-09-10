# CoreLibs

[![CI](https://github.com/deniscuciuc/dotnet-libs/actions/workflows/ci.yml/badge.svg)](https://github.com/deniscuciuc/dotnet-libs/actions/workflows/ci.yml)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)

> Shared .NET service infrastructure — consumed as a git submodule, not from NuGet.

Three libraries that most of my .NET services are built on. They are deliberately **not
published to NuGet**: they are opinionated, they move with the services that use them, and a
submodule lets a change land in a consumer the same day it is written rather than a release
later.

| Module | Namespace | What it is |
|---|---|---|
| [`platform/`](#platform) | `CoreLibs.*` | Configuration, startup orchestration, CQRS, persistence, jobs, storage, identity, observability |
| [`live-config/`](#liveconfig) | `CoreLibs.LiveConfig.*` | Hot-reloadable runtime configuration from JSON, Google Sheets, Redis, MongoDB and Postgres |
| [`localization/`](#localization) | `CoreLibs.Localization.*` | Database-backed runtime localization with LiveConfig and Telegram adapters |

Targets **net10.0**. Building needs the **.NET 10 SDK** (pinned in [`global.json`](global.json)).

## Consuming it

Add the repository as a submodule, then reference the projects you need by relative path.

```bash
git submodule add https://github.com/deniscuciuc/dotnet-libs libs/dotnet-libs
git submodule update --init --recursive
```

```xml
<ItemGroup>
  <ProjectReference Include="libs/dotnet-libs/platform/src/CoreLibs.Startup/CoreLibs.Startup.csproj" />
  <ProjectReference Include="libs/dotnet-libs/platform/src/CoreLibs.MongoDB/CoreLibs.MongoDB.csproj" />
  <ProjectReference Include="libs/dotnet-libs/live-config/src/CoreLibs.LiveConfig.Startup/CoreLibs.LiveConfig.Startup.csproj" />
</ItemGroup>
```

Pin the submodule to a tag rather than tracking `main` if you want reproducible builds:

```bash
git -C libs/dotnet-libs checkout v1.0.0
```

To update later:

```bash
git submodule update --remote libs/dotnet-libs
```

### Why not NuGet

These packages are shaped around one set of conventions — an ordered `IStartup` pipeline, a
YAML configuration layout, a MongoDB repository pattern. Publishing them would imply an API
stability promise that would slow the services down for no one's benefit. If you want a
standalone, versioned, `dotnet add package` library from this account, see
[BotForge](https://github.com/deniscuciuc/botforge),
[FluentDocs](https://github.com/deniscuciuc/fluentdocs) and
[Rulebook](https://github.com/deniscuciuc/rulebook) instead — none of those depends on this
repository.

## Platform

Configuration, host startup, and the infrastructure a service usually needs on day one.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddCoreConfiguration();          // YAML pipeline + convention-bound typed options
builder.Host.UseCoreSerilog();
builder.Services.AddCoreMongoDBStartup();
builder.Services.AddCoreCqrsFromAssemblyOf<Program>();

var app = builder.Build();
app.UseCoreCorrelationId();
app.MapCoreMetrics();

await app.RunWithStartupAsync();         // ordered startup tasks run before the host serves
```

| Project | Purpose |
|---|---|
| `CoreLibs.Configuration` | YAML configuration pipeline with convention-based typed options |
| `CoreLibs.Startup` | Ordered startup orchestration and fail-fast guards before `Host.RunAsync` |
| `CoreLibs.Domain` | `Entity`, domain events, auditing and soft-delete primitives |
| `CoreLibs.CQRS` | MediatR + FluentValidation + ErrorOr request pipeline with caching behaviours |
| `CoreLibs.Cache` | Cache abstractions shared by the Redis and Mongo layers |
| `CoreLibs.MongoDB` | Repository pattern, indexes, migrations, seeding, CAS/actor support |
| `CoreLibs.Postgres` | EF Core and Npgsql registration with naming conventions |
| `CoreLibs.Redis` | `ICache` over StackExchange.Redis, plus composite caching |
| `CoreLibs.MQ` | RabbitMQ via MassTransit |
| `CoreLibs.Identity` | JWT plus Firebase, Auth0, Telegram-login and internal-secret validators |
| `CoreLibs.Storage[.Disk|.S3]` | File storage facade with disk and S3 providers |
| `CoreLibs.Jobs[.Hangfire|.Quartz]` | Background jobs with MongoDB and Postgres stores |
| `CoreLibs.ErrorHandling` | Exception middleware and `ErrorOr` → `IResult` mapping |
| `CoreLibs.HealthCheck`, `.Metrics`, `.Serilog`, `.Sentry` | Observability |
| `CoreLibs.Swagger.Theme` | Swashbuckle registration with a dark theme |

Every component ships a matching `*.Startup` project that registers it into the startup
orchestrator. Docs: [`platform/docs/`](platform/docs/).

## LiveConfig

Configuration that changes while the process is running, without a redeploy.

Sources (`IConfigSource`) produce snapshots, `IConfigStore` versions them, `IConfigDistributor`
fans changes across instances over Redis pub/sub, and `IConfigApplier`/`IConfigHook`
implementations — discovered by attribute — apply them into typed caches.

| Project | Purpose |
|---|---|
| `CoreLibs.LiveConfig.Abstractions` | Contracts and attributes |
| `CoreLibs.LiveConfig` | Engine, hashing and snapshot pipeline |
| `CoreLibs.LiveConfig.Json` | JSON and YAML file sources |
| `CoreLibs.LiveConfig.GSheet` | Google Sheets source, entity API and schema mapping |
| `CoreLibs.LiveConfig.Redis` | Cross-instance distribution |
| `CoreLibs.LiveConfig.Store.MongoDB`, `.Store.Postgres` | Versioned persistence with retention |
| `CoreLibs.LiveConfig.Startup` | Auto-discovery and preload |
| `CoreLibs.LiveConfig.Hosting` | Standalone config-manager host with MassTransit |
| `CoreLibs.LiveConfig.Observability` | OpenTelemetry metrics |

Docs: [`live-config/docs/`](live-config/docs/). Runnable example:
[`live-config/examples/`](live-config/examples/).

## Localization

Runtime i18n backed by a database rather than resx, so translators change strings without a
build.

```csharp
services.AddCoreLocalization(o =>
{
    o.DefaultCulture = "en";
    o.FallbackChain = ["ro", "en"];
});

// later
localizer.Get("cart.empty", "ro");
localizer.GetPlural("cart.items", "ro", count: 3);
```

| Project | Purpose |
|---|---|
| `CoreLibs.Localization` | Dependency-free contracts and value types |
| `CoreLibs.Localization.Runtime` | Localizer, atomically-swapped cache, interpolation, CLDR pluralization |
| `CoreLibs.Localization.Cache` | Redis decorator for cross-instance sync and cold start |
| `CoreLibs.Localization.Store.MongoDB`, `.Store.Postgres` | Persistence |
| `CoreLibs.Localization.LiveConfig[.Json|.GSheet]` | Rebuild the cache from LiveConfig sources |
| `CoreLibs.Localization.Telegram` | Language detection from a Telegram user |

Docs: [`localization/docs/`](localization/docs/).

## Building and testing

```bash
dotnet restore CoreLibs.slnx
dotnet build CoreLibs.slnx -c Release
dotnet test CoreLibs.slnx -c Release
```

The integration suites use [Testcontainers](https://testcontainers.com/) and need a working
Docker or Podman socket. They start MongoDB, Postgres and Redis themselves — no local
services required.

## Third-party licensing

Two dependencies are worth knowing about before you ship something built on this:

- **Hangfire** (`CoreLibs.Jobs.Hangfire`) is **LGPL-3.0 with a commercial option**. Using it
  as an unmodified NuGet package in a closed-source service is generally fine; redistributing
  a modified Hangfire is not. If that is a problem, use `CoreLibs.Jobs.Quartz` instead —
  Quartz.NET is Apache-2.0 and the `IJobScheduler` abstraction is the same either way.
- **MediatR** (`CoreLibs.CQRS`) is pinned to **12.1.1**, the last Apache-2.0 release. v13
  moved to a paid commercial license. The pin is deliberate; do not let a dependency update
  cross it without a decision.

Everything else is MIT, Apache-2.0 or BSD.

## Contributing

Releases are git tags, not packages — see [docs/release-process.md](docs/release-process.md).

See [CONTRIBUTING.md](CONTRIBUTING.md), [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md) and
[SECURITY.md](SECURITY.md).

## License

[MIT](LICENSE)
