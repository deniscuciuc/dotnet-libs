# Changelog

All notable changes to this project will be documented in this file.
Format: [Keep a Changelog](https://keepachangelog.com)

This changelog tracks the public open-source repository state of DenisCuciuc.Platform.

## [Unreleased]

- No unreleased changes yet.

## [1.0.0] - 2026-05-03

### Added

- Published the first clean open-source DenisCuciuc.Platform snapshot targeting .NET 10.
- Added repo-level CI and manual release workflows, package metadata, docs, and runnable examples.

### Changed

- Migrated package and namespace roots from the legacy internal platform to `DenisCuciuc.Platform.*`.
- Replaced legacy `qg:` configuration sections with raw section names such as `MongoDB`, `Redis`, `Cqrs`, `Jobs`, `Identity:*`, and `Storage:*`.
- Switched release publishing to an explicit manual GitHub Actions workflow with opt-in NuGet and GitHub Release steps.
- Renamed `samples` to `examples` and updated the docs and example configs to match the current platform surface.
- Normalized remaining legacy `qgcore` identifiers in example databases, disk storage defaults, Quartz job groups, and job observability keys.