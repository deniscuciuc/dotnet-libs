# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).
Because this repository is consumed as a git submodule rather than from NuGet, versions are
tags on `main` and consumers move their submodule pointer to adopt one.

## [Unreleased]

## [1.0.1] - 2026-09-11

### Changed

- Dependencies brought current across all three modules, including `MongoDB.Driver` 3.11.2,
  `Npgsql` 10.0.3, EF Core 10.0.12, `StackExchange.Redis` 3.2.0, `Serilog.AspNetCore` 10.0.0,
  `Sentry` 6.10.0, OpenTelemetry 1.18.0, `Polly` 8.7.0, `Hangfire` 1.8.25 and
  `Testcontainers` 4.15.0.
- **MassTransit is pinned to 8.5.8 deliberately.** Version 9 dropped Apache-2.0 for a
  proprietary licence, the same move MediatR made at v13. 8.5.8 is the last Apache-2.0
  release. `MediatR` stays at 12.1.1 for the same reason.
- **Quartz is pinned to 3.17.1 deliberately.** Quartz 4 reworked its API — `IJob.Execute`
  returns `ValueTask` and takes a `CancellationToken`, `JobListenerSupport` is gone,
  `IJobExecutionContext` lost `Put`/`Get`, and `ScheduleJob` takes a `ScheduleJobOptions`.
  Adapting `CoreLibs.Jobs.Quartz` to it is a piece of work in its own right, not a dependency
  bump. 3.17.1 is current, maintained and Apache-2.0, with no open advisories.
- **`Microsoft.OpenApi` is held at 2.12.2 deliberately.** It is a transitive security pin for
  GHSA-v5pm-xwqc-g5wc (patched in 2.7.5), and version 3 breaks the ASP.NET Core OpenAPI
  source generator, which is built against 2.x.

## [1.0.0] - 2026-09-11

Initial release of CoreLibs: three libraries that most of my .NET services are built on —
`platform` (configuration, startup orchestration, CQRS, persistence, jobs, storage, identity,
observability), `live-config` (hot-reloadable runtime configuration) and `localization`
(database-backed runtime i18n).

### Notes for this release

- **Consumed as a git submodule.** Nothing here is published to NuGet; that is deliberate and
  the README explains why. For standalone, versioned packages see
  [TeleForge](https://github.com/deniscuciuc/teleforge),
  [FluentDocs](https://github.com/deniscuciuc/fluentdocs) and
  [Rulebook](https://github.com/deniscuciuc/rulebook), none of which depend on this repository.
- `ApiTokenAuthOptions.AudienceFormat` makes the audience accepted by
  `OrganizationApiKeyJwtTokenValidator` configurable rather than fixed, defaulting to
  `Service.Api.{environment}`.
- The convention-based options binder in `CoreLibs.Configuration` derives a section name by
  stripping a trailing `Options` suffix and a leading `Core` prefix, so `CoreSentryOptions`
  binds to the `Sentry` section.
- `CoreLibs.Jobs.Hangfire` brings in **Hangfire**, which is LGPL-3.0 with a commercial
  option. `CoreLibs.Jobs.Quartz` is the Apache-2.0 alternative behind the same
  `IJobScheduler` abstraction. `CoreLibs.CQRS` pins **MediatR 12.1.1**, the last Apache-2.0
  release; the pin is deliberate.

### Build and CI

- `TreatWarningsAsErrors` with `NuGetAudit` in `all` mode at `low` level, so a dependency
  with a published advisory fails the build. Three transitive advisories are pinned in the
  root `Directory.Packages.props` with their GHSA identifiers: `Microsoft.OpenApi`
  (GHSA-v5pm-xwqc-g5wc, high), `SSH.NET` (GHSA-q939-rpr3-3284, high) and `SharpCompress`
  (GHSA-6c8g-7p36-r338, moderate).
- CI builds and tests on Linux and Windows, collects coverage, and **runs the integration
  suites** — MongoDB, Postgres and Redis are started through Testcontainers.
- CodeQL, gitleaks secret scanning and Dependabot for NuGet and GitHub Actions.

### Testing

23 test suites, 668 tests. The localization module carries 48 of them, covering plural
resolution and its fallback chain, custom CLDR plural rules, `AddCoreLocalization`
registration semantics, and the cache's atomic-swap behaviour under concurrent readers and
writers.

The MongoDB integration suite pins `mongo:7.0` — `mongo:8.0` refuses to start on Linux 6.19
and newer ([SERVER-121912](https://jira.mongodb.org/browse/SERVER-121912)).

[Unreleased]: https://github.com/deniscuciuc/dotnet-libs/compare/v1.0.1...HEAD
[1.0.1]: https://github.com/deniscuciuc/dotnet-libs/compare/v1.0.0...v1.0.1
[1.0.0]: https://github.com/deniscuciuc/dotnet-libs/releases/tag/v1.0.0
