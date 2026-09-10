# Architecture

## Overview

CoreLibs LiveConfig is a modular framework for managing live configuration across distributed services. It follows a **source → engine → store → distribute → apply** pipeline.

## Core Pipeline

```
┌─────────────────┐
│  Config Source   │  Fetches raw data (Google Sheets, JSON files, APIs)
│  IConfigSource   │  Returns ConfigSnapshot (JSON + hash)
└────────┬────────┘
         ▼
┌─────────────────┐
│  LiveConfigEngine│  Central orchestrator
│  - hash compare  │  Computes SHA-256, skips no-ops
│  - versioning    │  Auto-increments, stores version history
│  - distribution  │  Pushes to Redis, publishes notifications
│  - hooks         │  Before/after apply extensibility
│  - apply         │  Deserializes into typed caches
└────────┬────────┘
         │
    ┌────┴────┐
    ▼         ▼
┌────────┐ ┌──────────┐
│ Store  │ │Distributor│
│ IConfig│ │IConfig    │
│ Store  │ │Distributor│
│(Mongo/ │ │ (Redis)   │
│Postgres)│ └─────┬────┘
└────────┘       │
           ┌─────┴──────┐
           ▼            ▼
     ┌──────────┐ ┌──────────┐
     │Consumer A│ │Consumer B│
     │(cached)  │ │(cached)  │
     └──────────┘ └──────────┘
```

## Package Dependency Graph

```
CoreLibs.LiveConfig.Abstractions          ← Zero dependencies (interfaces, models)
    ↑
CoreLibs.LiveConfig                       ← Core engine, hashers, registries
    ↑
├── CoreLibs.LiveConfig.Store.MongoDB     ← MongoDB IConfigStore
├── CoreLibs.LiveConfig.Store.Postgres    ← Postgres IConfigStore (EF Core)
├── CoreLibs.LiveConfig.Redis             ← Redis IConfigDistributor
├── CoreLibs.LiveConfig.GSheet            ← Google Sheets IConfigSource
├── CoreLibs.LiveConfig.Json              ← JSON file IConfigSource
├── CoreLibs.LiveConfig.Observability     ← Metrics + tracing (Abstractions only)
├── CoreLibs.LiveConfig.Startup           ← DI helpers, preload service
└── CoreLibs.LiveConfig.Hosting           ← MassTransit consumers, Redis subscriber
```

## Key Concepts

### Config Type

A string identifier for a configuration domain (e.g. `"bonuses"`, `"game-settings"`, `"translations"`). Each config type is versioned independently.

### Config Snapshot

A point-in-time fetch result from a source:

```csharp
public sealed record ConfigSnapshot(
    string ConfigType,
    string DataJson,     // Serialized JSON payload
    string DataHash,     // SHA-256 for no-op detection
    DateTimeOffset FetchedAt);
```

### Config Version

A persisted, versioned entry in the store:

```csharp
public sealed record ConfigVersion(
    string ConfigType,
    int Version,         // Auto-incremented
    string DataJson,
    string DataHash,
    bool IsActive,       // Only one active per type
    DateTimeOffset CreatedAt,
    string? CreatedBy);
```

### Import Pipeline

1. **Source** fetches raw data → `ConfigSnapshot`
2. **Engine** computes hash, compares with active version
3. If changed: **Store** creates new version, activates it
4. **Distributor** pushes JSON to Redis, publishes update notification
5. **Hooks** fire `BeforeApply` / `AfterApply`
6. **Appliers** deserialize JSON into typed `IConfigCache<T>` instances

### No-Op Detection

The engine computes SHA-256 of the serialized JSON. If the hash matches the active version, the import is skipped entirely — no new version, no distribution, no apply.

### Rollback

Any previous version can be re-activated:

```csharp
await engine.RollbackAsync("bonuses", targetVersion: 3, requestedBy: "admin");
```

This re-activates version 3, distributes it to Redis, and applies it to all consumers.

## Deployment Patterns

### Pattern 1: Single Service (simplest)

One service acts as both config manager and consumer:

```
┌─────────────────────┐
│  Service             │
│  ├─ GSheet Source    │
│  ├─ Mongo Store      │
│  ├─ Config Caches    │
│  └─ No Redis needed  │
└─────────────────────┘
```

### Pattern 2: Manager + Consumers (recommended)

Dedicated config manager with consumer subscribers:

```
┌──────────────┐           ┌──────────────┐
│ Config Manager│──Redis──▶│ API Service  │
│ (GSheet+Mongo│           │ (cached reads)│
│  +Redis+MQ)  │           └──────────────┘
└──────────────┘           ┌──────────────┐
       │          ──Redis──▶│ Worker       │
       │                    │ (cached reads)│
       │                    └──────────────┘
       │
   RabbitMQ (import/rollback commands)
```

### Pattern 3: Multi-Source

Multiple sources feed into one store:

```
┌─────────┐  ┌──────────┐
│ GSheet  │  │ JSON File│
│ Source  │  │ Source   │
└────┬────┘  └────┬─────┘
     └─────┬──────┘
           ▼
    ┌──────────────┐
    │LiveConfigEngine│
    └──────────────┘
```
