# LiveConfig Example Worker

Full lifecycle example demonstrating the CoreLibs LiveConfig framework with the CoreLibs platform bootstrap style.

## Prerequisites

- .NET 10 SDK
- Docker (for MongoDB, Redis, RabbitMQ)

## Quick Start

### 1. Start Infrastructure

```bash
docker compose up -d
```

This starts MongoDB, Redis, and RabbitMQ.

### 2. Run the Service

```bash
dotnet run
```

The service will:

1. Load sample JSON config files from `Sources/Json/`
2. Store versioned snapshots in MongoDB
3. Cache configs in Redis for fast access
4. Listen for import/rollback commands via RabbitMQ
5. Expose Swagger UI and REST endpoints for reading configs

### 3. Open the API

Swagger UI:

```bash
start http://localhost:5000/swagger
```

Health check:

```bash
curl http://localhost:5000/health
```

### 4. Try the Config Endpoints

Read cached bonuses:

```bash
curl http://localhost:5000/configs/bonuses
```

Read cached game settings:

```bash
curl http://localhost:5000/configs/game-settings
```

Trigger a manual import from the JSON sample source:

```bash
curl -X POST http://localhost:5000/configs/import
```

Run the tightly coupled transactional import:

```bash
curl -X POST http://localhost:5000/configs/import/transactional
```

View the active version manifest:

```bash
curl http://localhost:5000/configs/manifest
```

Rollback to a previous version:

```bash
curl -X POST http://localhost:5000/configs/bonuses/rollback/1
```

## Import Strategy Layout

The example is now grouped by how data gets imported instead of one flat folder.

- `Sources/Json/` contains the local file-based source used for the runnable demo.
- `Sources/GSheet/Legacy/` contains the original `IGSheetImporter` examples with explicit `ClassMap` and validation.
- `Sources/GSheet/Pipeline/` contains the recommended `IGSheetPipeline` examples with attributes, validators, aggregators, and dependencies.
- `Sources/GSheet/EntityApi/` contains the higher-level `GSheetEntity` / `GSheetEntityConfig` examples that show the declarative API.

## How It Works

### Import Flow

```text
JSON Files (Sources/Json/) ──▶ LiveConfigEngine ──▶ MongoDB (versioned store)
                                      │
                                      ├──▶ Redis (fast cache + pub/sub notify)
                                      │
                                      └──▶ IConfigCache<T> (in-memory typed cache)
```

### Consumer Flow

```text
Redis pub/sub notification ──▶ LiveConfigSubscriberService
                                        │
                                        ▼
                              engine.LoadAndApplyAsync()
                                        │
                                        ▼
                              IConfigCache<T> updated
```

## Using Google Sheets Instead

To switch from JSON files to Google Sheets:

1. Create a Google Cloud service account with Sheets API access.
2. Open [Sources/GSheet/prepare-example-data.gs](examples/CoreLibs.LiveConfig.Examples.Worker/Sources/GSheet/prepare-example-data.gs) and paste it into `Extensions > Apps Script` in your spreadsheet.
3. Run `prepareLiveConfigExampleSheets()` once to create and fill all example sheets.
4. Base64-encode the service account JSON.
5. Set the `GOOGLE_SHEET_CREDENTIALS` environment variable.
6. Uncomment the GSheet section in `appsettings.yml`.
7. In `Program.cs`, enable `AddLiveConfigGSheet(...)` and disable the JSON source.
8. Pick the example style you want to study from `Sources/GSheet/Legacy`, `Sources/GSheet/Pipeline`, or `Sources/GSheet/EntityApi`.

## Project Structure

```text
├── Program.cs                          # Web API bootstrap, platform startup, routes
├── Domain/
│   ├── BonusConfig.cs                  # Simple independent config DTO
│   ├── GameSettingsConfig.cs           # Simple key/value config DTO
│   └── CatalogConfigs.cs               # Cross-config DTOs for currencies, items, tournaments
├── Sources/
│   ├── Json/
│   │   ├── bonuses.json                # Runnable local sample data
│   │   └── game-settings.json          # Runnable local sample data
│   └── GSheet/
│       ├── Legacy/                     # Original IGSheetImporter examples
│       ├── Pipeline/                   # IGSheetPipeline examples
│       └── EntityApi/                  # GSheetEntity / GSheetEntityConfig examples
├── appsettings.yml                     # LiveConfig + Serilog + infrastructure config
├── docker-compose.yml                  # MongoDB + Redis + RabbitMQ
└── README.md
```
