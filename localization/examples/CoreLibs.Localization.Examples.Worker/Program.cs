using CoreLibs.LiveConfig;
using CoreLibs.LiveConfig.GSheet;
using CoreLibs.LiveConfig.Startup;
using CoreLibs.Localization;
using CoreLibs.Localization.Cache;
using CoreLibs.Localization.Examples.Worker.Entities;
using CoreLibs.Localization.Examples.Worker.Importers;
using CoreLibs.Localization.Examples.Worker.Services;
using CoreLibs.Localization.LiveConfig;
using CoreLibs.Localization.LiveConfig.GSheet;
using CoreLibs.Localization.LiveConfig.Json;
using CoreLibs.Localization.Runtime;
using CoreLibs.Localization.Store.MongoDB;
using CoreLibs.Localization.Store.Postgres;
using CoreLibs.Localization.Telegram;
using CoreLibs.MongoDB;
using CoreLibs.MongoDB.Startup;
using CoreLibs.Redis;
using CoreLibs.Redis.Startup;
using CoreLibs.Startup;
using NetEscapades.Configuration.Yaml;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddYamlFile("appsettings.yml", false, true);

var cultures = builder.Configuration.GetSection("Example:Cultures").Get<string[]>() ?? ["ru", "en"];
var sourceMode = (builder.Configuration["Example:SourceMode"] ?? "json").Trim().ToLowerInvariant();
var storeEnabled = builder.Configuration.GetValue<bool>("Example:StoreEnabled");
var storeType = (builder.Configuration["Example:StoreType"] ?? "mongo").Trim().ToLowerInvariant();
var redisEnabled = builder.Configuration.GetValue<bool>("Example:RedisCacheEnabled");
var liveConfigEnabled = builder.Configuration.GetValue<bool>("Example:LiveConfigEnabled");

builder.Services.AddCoreLocalization(opts =>
    builder.Configuration.GetSection(LocalizationOptions.SectionPath).Bind(opts));

builder.Services.AddTransient(sp =>
    new LocalizationJsonLoader(
        sp.GetRequiredService<IWebHostEnvironment>().ContentRootFileProvider,
        sp.GetRequiredService<ILogger<LocalizationJsonLoader>>()));

if (sourceMode == "json")
    builder.Services.AddHostedService<LocalizationWarmupService>();

builder.Services.AddCoreLocalizationLiveConfig(cultures);

if (storeEnabled)
{
    if (storeType == "postgres")
    {
        var pgCs = builder.Configuration["Postgres:ConnectionString"]
                   ?? throw new InvalidOperationException(
                       "Postgres:ConnectionString is required when StoreType = postgres.");
        builder.Services.AddCoreLocalizationPostgres(pgCs);
    }
    else
    {
        var mongoOptions = builder.Configuration.GetSection("MongoDB").Get<MongoDBOptions>()
                           ?? new MongoDBOptions();
        builder.Services.AddMongoDB(mongoOptions);
        builder.Services.AddCoreMongoDBStartup();
        builder.Services.AddCoreLocalizationMongo();
    }

    builder.Services.AddTransient<SnapshotStoreImporter>();

    if (sourceMode == "store")
        builder.Services.AddHostedService<LocalizationStoreWarmupService>();
}

if (redisEnabled)
{
    builder.Services.AddCoreRedis(builder.Configuration.GetSection("Redis"));
    builder.Services.AddCoreRedisStartup();
    builder.Services.AddCoreLocalizationRedisCache(opts =>
        builder.Configuration.GetSection(LocalizationCacheOptions.SectionPath).Bind(opts));
}

builder.Services.AddSingleton<InMemoryTelegramUserContext>();
builder.Services.AddSingleton<ITelegramUserContext>(sp =>
    sp.GetRequiredService<InMemoryTelegramUserContext>());
builder.Services.AddSingleton<TelegramLocalizer>();

if (liveConfigEnabled)
{
    builder.Services.AddLiveConfig(builder.Configuration);

    builder.Services.AddSingleton<IConfigSource>(sp =>
        new LocalizationJsonConfigSource(
            sp.GetRequiredService<IWebHostEnvironment>().ContentRootFileProvider,
            sp.GetRequiredService<ILoggerFactory>(),
            LocalizationConstants.DefaultLocalesDirectory));

    var configSourceCountBefore = builder.Services.Count(sd => sd.ServiceType == typeof(IConfigSource));
    builder.Services.AddLiveConfigGSheet(builder.Configuration);
    builder.Services.AddGSheetImporter<LocalizationGSheetImporter>();
    var frameworkSources = builder.Services
        .Where(sd => sd.ServiceType == typeof(IConfigSource))
        .Skip(configSourceCountBefore)
        .ToList();
    foreach (var d in frameworkSources)
        builder.Services.Remove(d);

    builder.Services.AddSingleton<IConfigSource, LocalizationGSheetConfigSource>();

    builder.Services.AddSingleton<IConfigStore, InMemoryConfigStore>();
    builder.Services.AddCoreLiveConfigStartup();
}

var app = builder.Build();

// GET /localize?key=app.welcome&culture=ru
app.MapGet("/localize", (ILocalizer localizer, string key, string culture) =>
    Results.Ok(new { key, culture, value = localizer.Get(key, culture) }));

// GET /localize/{key}/{culture}
app.MapGet("/localize/{key}/{culture}", (ILocalizer localizer, string key, string culture) =>
    Results.Ok(new { key, culture, value = localizer.Get(key, culture) }));

// GET /localize/plural?key=game.items.count&culture=ru&count=5
//   Uses CLDR plural categories: zero / one / few / many / other
app.MapGet("/localize/plural", (ILocalizer localizer, string key, string culture, long count) =>
    Results.Ok(new { key, culture, count, value = localizer.GetPlural(key, culture, count) }));

// GET /localize/interpolate?key=offers.bonus.received&culture=en&amount=100&currency=USD
//   Any extra query params are forwarded as interpolation arguments: {amount}, {currency}, etc.
app.MapGet("/localize/interpolate", (ILocalizer localizer, string key, string culture, HttpRequest request) =>
{
    var args = request.Query
        .Where(q => q.Key is not ("key" or "culture"))
        .ToDictionary(q => q.Key, q => q.Value.ToString()!);
    return Results.Ok(new { key, culture, args, value = localizer.Get(key, culture, args) });
});

// GET /localize/all/{culture} — dump all loaded keys for a given culture
app.MapGet("/localize/all/{culture}", (ILocalizationCache cache, string culture) =>
{
    var provider = cache.GetProvider(culture);
    return provider is null
        ? Results.NotFound(new { error = $"Culture '{culture}' is not loaded." })
        : Results.Ok(new { culture, count = provider.GetAll().Count, entries = provider.GetAll() });
});

// GET /cache/status
app.MapGet("/cache/status", (ILocalizationCache cache) =>
{
    var loaded = cache.GetLoadedCultures();
    return Results.Ok(new
    {
        loadedCultures = loaded,
        details = loaded.Select(c => new
        {
            culture = c,
            entryCount = cache.GetProvider(c)?.GetAll().Count ?? 0
        })
    });
});

// POST /cache/reload — re-read locale JSON files and atomically replace all cultures
app.MapPost("/cache/reload", (LocalizationJsonLoader loader, ILocalizationCache cache) =>
{
    var reloaded = new List<string>();
    foreach (var snapshot in loader.LoadAll(LocalizationConstants.DefaultLocalesDirectory))
    {
        cache.Update(snapshot.Culture, snapshot.Entries);
        reloaded.Add(snapshot.Culture);
    }

    return Results.Ok(new { reloaded });
});

// POST /cache/apply — simulate a LiveConfig push; deserializes a LocalizationSnapshot and loads it
app.MapPost("/cache/apply", (ILocalizationCache cache, LocalizationSnapshot snapshot) =>
{
    if (string.IsNullOrWhiteSpace(snapshot.Culture))
        return Results.BadRequest(new { error = "snapshot.culture is required." });

    cache.Update(snapshot.Culture, snapshot.Entries);
    return Results.Ok(new
    {
        applied = snapshot.Culture,
        entries = snapshot.Entries.Count
    });
});

// GET /telegram/users
app.MapGet("/telegram/users", (InMemoryTelegramUserContext ctx) =>
    Results.Ok(ctx.All.Select(kv => new { userId = kv.Key, culture = kv.Value })));

// GET /telegram/users/{userId}
app.MapGet("/telegram/users/{userId:long}", (InMemoryTelegramUserContext ctx, long userId) =>
    async () => Results.Ok(new { userId, culture = await ctx.GetUserCultureAsync(userId) ?? "(not set — uses default)" }));

// PUT /telegram/users/{userId}/culture  body: {"culture":"ru"}
app.MapPut("/telegram/users/{userId:long}/culture",
    (InMemoryTelegramUserContext ctx, long userId, CultureRequest req) =>
    {
        ctx.Set(userId, req.Culture);
        return Results.Ok(new { userId, culture = req.Culture });
    });

// DELETE /telegram/users/{userId}/culture — resets to default
app.MapDelete("/telegram/users/{userId:long}/culture",
    (InMemoryTelegramUserContext ctx, long userId) =>
    {
        ctx.Remove(userId);
        return Results.NoContent();
    });

// GET /telegram/users/{userId}/localize?key=app.greeting
//   Culture is resolved automatically from the user context (or falls back to default).
app.MapGet("/telegram/users/{userId:long}/localize",
    async (TelegramLocalizer localizer, long userId, string key) =>
        Results.Ok(new { userId, key, value = (await localizer.ForUserAsync(userId)).Get(key) }));

// GET /telegram/users/{userId}/localize/{key}
app.MapGet("/telegram/users/{userId:long}/localize/{key}",
    async (TelegramLocalizer localizer, long userId, string key) =>
        Results.Ok(new { userId, key, value = (await localizer.ForUserAsync(userId)).Get(key) }));

if (storeEnabled)
{
    // GET /store/entries/{culture}
    app.MapGet("/store/entries/{culture}", async (
        ILocalizationStore store, string culture, CancellationToken ct) =>
    {
        var entries = await store.FindAllAsync(culture, ct);
        return Results.Ok(entries);
    });

    // POST /store/entries — upsert a single entry
    app.MapPost("/store/entries", async (
        ILocalizationStore store, LocalizationEntry entry, CancellationToken ct) =>
    {
        await store.UpsertAsync(entry, ct);
        return Results.Ok(new { upserted = entry });
    });

    // DELETE /store/entries/{culture}/{key}
    app.MapDelete("/store/entries/{culture}/{key}", async (
        ILocalizationStore store, string culture, string key, CancellationToken ct) =>
    {
        await store.DeleteAsync(key, culture, ct);
        return Results.NoContent();
    });

    // POST /store/import — bulk-import a full LocalizationSnapshot into MongoDB
    //   Also accepts the output of LocalizationJsonLoader or LocalizationGSheetConverter.
    app.MapPost("/store/import", async (
        SnapshotStoreImporter importer, LocalizationSnapshot snapshot, CancellationToken ct) =>
    {
        await importer.ImportAsync(snapshot, ct);
        return Results.Ok(new { imported = snapshot.Entries.Count, culture = snapshot.Culture });
    });
}

if (liveConfigEnabled)
{
    app.MapPost("/liveconfig/import", async (
        LiveConfigEngine engine, ConfigSourceRegistry sourceRegistry, CancellationToken ct) =>
    {
        var results = new List<ConfigImportResult>();
        foreach (var source in sourceRegistry.All.Where(s => s.SourceName != LocalizationConstants.JsonSourceName))
        {
            var snapshots = await source.FetchAllAsync(ct);
            var batch = await engine.ImportBatchAsync(snapshots, createdBy: "api", cancellationToken: ct);
            results.AddRange(batch.Results);
        }

        return Results.Ok(new { results });
    });

    // POST /liveconfig/import/{sourceName} — import from a specific source
    app.MapPost("/liveconfig/import/{sourceName}", async (
        string sourceName, LiveConfigEngine engine, ConfigSourceRegistry sourceRegistry, CancellationToken ct) =>
    {
        var source = sourceRegistry.Resolve(sourceName);
        if (source is null)
            return Results.NotFound(new
            { error = $"Source '{sourceName}' not found.", available = sourceRegistry.SourceNames });

        var snapshots = await source.FetchAllAsync(ct);
        var batch = await engine.ImportBatchAsync(snapshots, createdBy: "api", cancellationToken: ct);
        return Results.Ok(new { results = batch.Results });
    });

    // GET /liveconfig/manifest — list all active config types and versions
    app.MapGet("/liveconfig/manifest", async (LiveConfigEngine engine, CancellationToken ct) =>
        Results.Ok(await engine.GetManifestAsync(ct)));

    // POST /liveconfig/{configType}/rollback/{version} — rollback to a previous version
    app.MapPost("/liveconfig/{configType}/rollback/{version:int}", async (
        string configType, int version, LiveConfigEngine engine, CancellationToken ct) =>
    {
        var result = await engine.RollbackAsync(configType, version, "api", ct);
        return result.Changed ? Results.Ok(result) : Results.BadRequest(result);
    });

    // GET /liveconfig/sources — list registered config sources
    app.MapGet("/liveconfig/sources", (ConfigSourceRegistry sourceRegistry) =>
        Results.Ok(new { sources = sourceRegistry.SourceNames }));
}

await app.RunWithStartupAsync();

internal record CultureRequest(string Culture);
