# Observability

Three complementary libraries covering structured logging, error tracking, and metrics.

---

## CoreLibs.Serilog

Structured logging for ASP.NET Core with log enrichment, output templates, and environment-aware configuration.

### Setup

```csharp
builder.Host.UseCoreSerilog();
```

**appsettings.yml:**

```yaml
Serilog:
  MinimumLevel:
    Default: Information
    Override:
      Microsoft: Warning
      System: Warning
  WriteTo:
    - Name: Console
      Args:
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"
  Enrich:
    - FromLogContext
    - WithMachineName
    - WithEnvironmentName
```

---

## CoreLibs.Sentry

Sentry error tracking integrated with OpenTelemetry for distributed traces.

### Setup

```csharp
builder.AddCoreSentry();
```

**appsettings.yml:**

```yaml
sentry:
  Dsn: "https://xxx@sentry.io/yyy"
  Environment: production
  TracesSampleRate: 0.1
  Debug: false
```

Sentry is connected to the OpenTelemetry pipeline — unhandled exceptions are automatically captured and linked to their trace.

---

## CoreLibs.Metrics

OpenTelemetry metrics exported via Prometheus, with optional Grafana dashboards.

### Setup

```csharp
builder.Services.AddCoreMetrics(builder.Configuration);

var app = builder.Build();
app.MapCoreMetrics();  // exposes /metrics by default
```

**appsettings.yml:**

```yaml
metrics:
  enabled: true
  endpointPath: /metrics
  enableAspNetCoreInstrumentation: true
  meters:
    - CoreLibs.MongoDB
    - CoreLibs.Redis
```

### Built-in metrics

Each infrastructure library contributes its own meters once registered:

| Library | Metrics |
|---|---|
| `CoreLibs.MongoDB` | `mongodb.operation.duration`, connection pool stats |
| `CoreLibs.Redis` | Operation latency, cache hit/miss rate |
| `CoreLibs.MQ` | Message publish/consume rate, processing duration |
| ASP.NET Core | Request duration, error rate (via OpenTelemetry.Instrumentation.AspNetCore) |

### Custom metrics

```csharp
public class OrderService
{
    private static readonly Counter<long> OrdersCreated =
        Meter.CreateCounter<long>("orders.created");

    public async Task PlaceOrderAsync(Order order)
    {
        await repo.InsertOneAsync(order);
        OrdersCreated.Add(1, new TagList { { "currency", order.Currency } });
    }
}
```

---

## CoreLibs.HealthCheck

Health check endpoint that aggregates all registered checks.

### Setup

```csharp
builder.Services.AddHealthChecks()
    .AddMongoDBHealthCheck()
    .AddRedisHealthCheck();

app.UseHealthChecks(); // → GET /health
```

### Response

```json
{
  "status": "Healthy",
  "results": {
    "mongodb": { "status": "Healthy" },
    "redis": { "status": "Healthy" }
  }
}
```

Returns `200 OK` when healthy, `503 Service Unavailable` otherwise. Compatible with Kubernetes liveness/readiness probes and load balancer health checks.
