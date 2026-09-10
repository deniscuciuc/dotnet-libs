# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).
Because this repository is consumed as a git submodule rather than from NuGet, versions are
tags on `main` and consumers move their submodule pointer to adopt one.

## [Unreleased]

## [2.0.0] - 2026-09-10

First public release. Extracted from a private monorepo, relicensed under MIT, and renamed
throughout. This is a breaking release for anyone who was consuming the private version.

### Breaking

- Every namespace, project, folder and assembly renamed from `QGCore.*` to `CoreLibs.*`.
  `QGCore.MongoDB` is now `CoreLibs.MongoDB`, `QGCore.LiveConfig.Store.Postgres` is now
  `CoreLibs.LiveConfig.Store.Postgres`, and so on.
- Extension methods renamed to match: `AddQG*` → `AddCore*`, `UseQG*` → `UseCore*`,
  `MapQG*` → `MapCore*`. `IQGJob` is now `ICoreJob` and `QG*Options` are now `Core*Options`.
- The convention-based options binder in `CoreLibs.Configuration` now strips a leading
  `Core` prefix when deriving a section name, where it previously stripped `QG` and
  `Platform`. Section names on disk are unchanged — `CoreSentryOptions` still binds to
  `Sentry`.
- `CoreLibs.Swagger` is gone. It was a project with no source files that existed only to
  aggregate four Swashbuckle package references, and it packed as an empty assembly. Those
  references now live in `CoreLibs.Swagger.Theme`, which is where `UseCoreSwagger` already
  was — reference that instead.

### Added

- MIT license. The previous copy claimed PolyForm Strict, which is source-available rather
  than open source, and shipped no `LICENSE` file at all.
- `ApiTokenAuthOptions.AudienceFormat`, so `OrganizationApiKeyJwtTokenValidator` no longer
  hardcodes the audience it accepts. The default keeps the previous
  `Service.Api.{environment}` behaviour.
- 23 localization tests covering plural resolution and its fallback chain, custom CLDR plural
  rules, `AddCoreLocalization` registration semantics, and the cache's atomic-swap behaviour
  under concurrent readers and writers. Localization was the least-covered module in the
  repository.
- CI that actually runs the integration suites. Six Testcontainers-based projects existed but
  no job had ever executed them; MongoDB, Postgres and Redis now start in CI. Builds and unit
  tests also run on Windows, and coverage is collected.
- CodeQL, gitleaks secret scanning, Dependabot for NuGet and GitHub Actions, `CODEOWNERS`,
  `SECURITY.md`, `CODE_OF_CONDUCT.md`, `.gitattributes` and issue and PR templates.

### Fixed

- `NU1902` was suppressed repo-wide, hiding moderate-severity dependency advisories. The
  suppression is gone and `NuGetAudit` now runs in `all` mode at `low` level with warnings as
  errors. Removing it immediately surfaced three real transitive advisories, now pinned in
  the root `Directory.Packages.props`: `Microsoft.OpenApi` (GHSA-v5pm-xwqc-g5wc, high),
  `SSH.NET` (GHSA-q939-rpr3-3284, high) and `SharpCompress` (GHSA-6c8g-7p36-r338, moderate).
- The MongoDB integration suite pinned `mongo:8.0`, which refuses to start on Linux 6.19 and
  newer ([SERVER-121912](https://jira.mongodb.org/browse/SERVER-121912)) — all 62 tests
  failed at container startup. Now pinned to `mongo:7.0`.
- `PackageProjectUrl` was computed from the second segment of the project name, producing
  URLs for around thirty repositories that do not exist, and needed a `.Contains('.')` guard
  to stop MSB4184 crashing on projects without a dot in their name. Removed — nothing here is
  packed.
- `DiskStorageOptions.BasePath` no longer defaults to a path containing a personal brand
  name, and the example databases are named after the library rather than its author.

### Removed

- The `scraping` module, which is not being open sourced.
- `.gitlab-ci.yml`, which included a CI template from a private GitLab group and could never
  have run here.
- Per-module `VERSION` files and their eight version-bumping shell scripts, replaced by
  ordinary git tags.
- Documentation pointing at a private GitHub Packages feed, rewritten for submodule
  consumption.

[Unreleased]: https://github.com/deniscuciuc/dotnet-libs/compare/v2.0.0...HEAD
[2.0.0]: https://github.com/deniscuciuc/dotnet-libs/releases/tag/v2.0.0
