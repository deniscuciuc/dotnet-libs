using CoreLibs.Configuration;
using CoreLibs.LiveConfig;
using CoreLibs.LiveConfig.Examples.Worker;
using CoreLibs.LiveConfig.Examples.Worker.Consumers;
using CoreLibs.LiveConfig.Examples.Worker.Domain;
using CoreLibs.LiveConfig.GSheet;
using CoreLibs.LiveConfig.GSheet.Pipeline;
using CoreLibs.LiveConfig.Hosting;
using CoreLibs.LiveConfig.Json;
using CoreLibs.LiveConfig.Redis;
using CoreLibs.LiveConfig.Startup;
using CoreLibs.LiveConfig.Store.MongoDB;
using CoreLibs.MongoDB;
using CoreLibs.MongoDB.Startup;
using CoreLibs.Redis;
using CoreLibs.Redis.Startup;
using CoreLibs.Startup;
using MassTransit;
using Serilog;


var builder = WebApplication.CreateBuilder(args);

// ──────────────────────────────────────────────
// CoreLibs Infrastructure
// ──────────────────────────────────────────────
builder.AddCoreConfiguration();
builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration));

var mongoOptions = builder.Configuration.GetSection("MongoDB").Get<MongoDBOptions>() ?? new MongoDBOptions();
builder.Services.AddMongoDB(mongoOptions);
builder.Services.AddCoreMongoDBStartup();

builder.Services.AddCoreRedis(builder.Configuration.GetSection("Redis"));
builder.Services.AddCoreRedisStartup();

builder.Services.AddHealthChecks();
builder.Services.AddOpenApi();

var sourceMode = (builder.Configuration["Example:SourceMode"] ??
                  (string.IsNullOrWhiteSpace(builder.Configuration["qg:liveconfig:gsheet:spreadsheetId"])
                      ? "json"
                      : "gsheet"))
    .Trim()
    .ToLowerInvariant();

var activeSourceName = sourceMode switch
{
    "gsheet" => "gsheet",
    "json" => "json-file",
    _ => throw new InvalidOperationException(
        $"Unsupported Example:SourceMode '{sourceMode}'. Use 'json' or 'gsheet'.")
};

// ──────────────────────────────────────────────
// 1. Core LiveConfig Framework
// ──────────────────────────────────────────────
builder.Services.AddLiveConfig(builder.Configuration);

// ──────────────────────────────────────────────
// 2. Config Sources
// ──────────────────────────────────────────────

if (sourceMode == "gsheet")
{
    builder.Services.AddLiveConfigGSheet(builder.Configuration);
    builder.Services.AddGSheetImportersInAssemblyOf<Program>();
    builder.Services.AddGSheetPipelineObserver<LoggingPipelineObserver>();
}
else
{
    builder.Services.AddLiveConfigJsonSource(opts =>
    {
        opts.Directory = Path.Combine(builder.Environment.ContentRootPath, "Sources", "Json");
        opts.Environment = builder.Environment.EnvironmentName;
    });
}

// ──────────────────────────────────────────────
// 3. Config Store — MongoDB
// ──────────────────────────────────────────────
builder.Services.AddLiveConfigMongoStore(builder.Configuration);

// ──────────────────────────────────────────────
// 4. Distributor — Redis (pub/sub + cache)
// ──────────────────────────────────────────────
builder.Services.AddLiveConfigRedis(builder.Configuration);

// ──────────────────────────────────────────────
// 5. Typed Config Caches
// ──────────────────────────────────────────────

// Simple configs (independent, eventually consistent)
builder.Services.AddLiveConfigCachedConfig<BonusConfig>("bonuses");
builder.Services.AddLiveConfigCachedConfig<GameSettingsConfig>("game-settings");
builder.Services.AddLiveConfigCachedConfig<CurrencyConfig>("Currencies");
builder.Services.AddLiveConfigCachedConfig<TournamentConfig>("Tournaments");

// Tightly coupled configs (import as a transactional batch)
builder.Services.AddLiveConfigCachedConfig<ItemConfig>("Items");
builder.Services.AddLiveConfigCachedConfig<ItemPriceConfig>("ItemPrices");
builder.Services.AddLiveConfigCachedConfig<ItemBundleConfig>("ItemBundles");

// ──────────────────────────────────────────────
// 6. Hosting — MQ Consumers + Redis Subscriber
// ──────────────────────────────────────────────
builder.Services.AddLiveConfigHosting(builder.Configuration);

builder.Services.AddSingleton<QueueResultTracker>();

builder.Services.AddMassTransit(x =>
{
    x.AddLiveConfigConsumers();
    x.AddConsumer<ConfigImportCompletedConsumer>();
    x.AddConsumer<ConfigRollbackCompletedConsumer>();
    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMq:Host"] ?? "localhost");
        ctx.ConfigureLiveConfigEndpoints(cfg);
        cfg.ConfigureEndpoints(ctx);
    });
});

// ──────────────────────────────────────────────
// 7. CoreLibs startup task — preloads critical configs before traffic
// ──────────────────────────────────────────────
builder.Services.AddCoreLiveConfigStartup();

var app = builder.Build();

// ──────────────────────────────────────────────
// Middleware & Endpoints
// ──────────────────────────────────────────────

app.UseSerilogRequestLogging();
app.MapOpenApi();
app.MapHealthChecks("/health");

app.MapGet("/", () => Results.Ok(new
{
    service = "CoreLibs.LiveConfig.Examples.Worker",
    openApi = "/openapi/v1.json",
    sourceMode,
    activeSource = activeSourceName,
    endpoints = new
    {
        direct = new[]
        {
            "POST /configs/import",
            "POST /configs/import/{sourceName}",
            "POST /configs/{configType}/rollback/{version}",
            "GET  /configs/manifest",
            "GET  /configs/bonuses",
            "GET  /configs/game-settings",
            "GET  /configs/tournaments",
            "GET  /configs/items"
        },
        queue = new[]
        {
            "POST /queue/import                          — publish import request to MQ",
            "POST /queue/import/{importerName}           — publish import for specific source",
            "POST /queue/rollback/{configType}/{version} — publish rollback request to MQ",
            "GET  /queue/results                         — recent MQ import/rollback results",
            "GET  /queue/results/imports                 — recent import results only",
            "GET  /queue/results/rollbacks               — recent rollback results only"
        }
    }
}));

// ─── LiveConfig direct endpoints (import, rollback, manifest) ─

app.MapLiveConfigEndpoints();

// ─── LiveConfig queue endpoints (MQ-based async) ─

app.MapLiveConfigQueueEndpoints();

// ─── Read cached configs (example-specific) ──

app.MapGet("/configs/bonuses", (IConfigCache<IReadOnlyList<BonusConfig>> cache) =>
    cache.Current is not null
        ? Results.Ok(new { Version = cache.CurrentVersion, Data = cache.Current })
        : Results.NotFound("Bonuses config not loaded yet"));

app.MapGet("/configs/game-settings", (IConfigCache<IReadOnlyList<GameSettingsConfig>> cache) =>
    cache.Current is not null
        ? Results.Ok(new { Version = cache.CurrentVersion, Data = cache.Current })
        : Results.NotFound("Game settings config not loaded yet"));

app.MapGet("/configs/tournaments", (IConfigCache<IReadOnlyList<TournamentConfig>> cache) =>
    cache.Current is not null
        ? Results.Ok(new { Version = cache.CurrentVersion, Data = cache.Current })
        : Results.NotFound("Tournament config not loaded yet"));

app.MapGet("/configs/items", (IConfigCache<IReadOnlyList<ItemConfig>> cache) =>
    cache.Current is not null
        ? Results.Ok(new { Version = cache.CurrentVersion, Data = cache.Current })
        : Results.NotFound("Item config not loaded yet"));

// ─── Queue result tracker (example-specific) ─

app.MapGet("/queue/results", (QueueResultTracker tracker) =>
    Results.Ok(new
    {
        imports = tracker.RecentImports,
        rollbacks = tracker.RecentRollbacks
    }));

app.MapGet("/queue/results/imports", (QueueResultTracker tracker) =>
    Results.Ok(tracker.RecentImports));

app.MapGet("/queue/results/rollbacks", (QueueResultTracker tracker) =>
    Results.Ok(tracker.RecentRollbacks));

// ──────────────────────────────────────────────
// Startup & Run (MongoDB indexes + Redis connect)
// ──────────────────────────────────────────────
await app.RunWithStartupAsync();
