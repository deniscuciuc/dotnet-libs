# Observability

The `CoreLibs.LiveConfig.Observability` package provides OpenTelemetry metrics and distributed tracing for the LiveConfig pipeline.

## Setup

The observability package is automatically referenced by the core engine. Metrics are recorded without additional configuration.

For explicit metric export (e.g. Prometheus):

```csharp
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddMeter("CoreLibs.LiveConfig");
    });
```

## Metrics

### Counters

| Metric | Description |
|---|---|
| `liveconfig.imports.total` | Total import operations (tags: `config_type`, `changed`) |
| `liveconfig.rollbacks.total` | Total rollback operations (tags: `config_type`, `success`) |
| `liveconfig.applies.total` | Total apply operations (tags: `config_type`) |
| `liveconfig.errors.total` | Total error count (tags: `config_type`, `operation`) |

### Histograms

| Metric | Description |
|---|---|
| `liveconfig.import.duration` | Import duration in milliseconds |
| `liveconfig.apply.duration` | Apply duration in milliseconds |

### Gauges

| Metric | Description |
|---|---|
| `liveconfig.active.versions` | Number of active config types |

## Distributed Tracing

The `LiveConfigActivitySource` creates spans for key operations:

| Activity | Description |
|---|---|
| `LiveConfig.Import` | Full import pipeline (hash → store → distribute → apply) |
| `LiveConfig.Rollback` | Version rollback |
| `LiveConfig.LoadAndApply` | Load from distributor/store and apply |
| `LiveConfig.GSheet.Orchestrate` | GSheet batch import orchestration |

To enable tracing:

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.AddSource("CoreLibs.LiveConfig");
    });
```
