# Entity API

The Entity API is the simplest way to import Google Sheets data. It uses a declarative, attribute-driven approach inspired by EF Core — define your domain model, add a few attributes, and you're done.

## Quick Start

```csharp
// 1. Define your entity
[GSheetEntity("Currencies")]  // range is optional — Google Sheets returns all filled columns
public record CurrencyEntity(
    [property: GSheetColumn, GSheetRowKey, GSheetRequired] string Code,
    [property: GSheetColumn, GSheetRequired] string Name,
    [property: GSheetColumn, GSheetRange(0, 8)] int DecimalPlaces);

// 2. Register at startup
builder.Services.AddLiveConfigGSheet(builder.Configuration);
builder.Services.AddGSheetEntitiesInAssemblyOf<CurrencyEntity>();
```

That's it. The orchestrator discovers the entity, parses the sheet, runs attribute validation, and stores the domain data — zero boilerplate.

---

## Three Tiers

| Tier | What you write | When to use |
|---|---|---|
| **1 — Attribute-only** | `[GSheetEntity]` on a record | Simple 1:1 sheet-to-model with attribute validation |
| **2 — Attribute + Config** | `[GSheetEntity]` + `GSheetEntityConfig<T>` | Custom validation rules, ordering, transforms |
| **3 — Full Config** | `GSheetEntityConfig<TRow, TDomain>` | Separate row/domain types, relationships, aggregation |

---

## Tier 1 — Attribute-Only

The simplest tier. Place `[GSheetEntity]` on a record and use schema attributes for validation.

The range parameter is **optional** — omit it and Google Sheets returns all filled columns automatically. Specify a range (e.g. `"A1:D"`) only when you want to limit which columns are fetched:

```csharp
[GSheetEntity("Items")]              // reads all columns
// or
// [GSheetEntity("Items", "A1:D")]  // reads only columns A–D
public record ItemEntity(
    [property: GSheetColumn("ItemId"), GSheetRowKey, GSheetRequired] string ItemId,
    [property: GSheetColumn("Name"), GSheetRequired] string Name,
    [property: GSheetColumn("Rarity"), GSheetRequired] string Rarity,
    [property: GSheetColumn("Category"), GSheetRequired] string Category);
```

**What happens automatically:**
- Column mapping via `[GSheetColumn]`
- Structural validation (header checking)
- Attribute validation (`[GSheetRequired]`, `[GSheetRange]`, `[GSheetRegex]`)
- Reference validation (`[GSheetRef]`)
- Identity mapping (the record IS the domain model)

**No config class, no ClassMap, no interface.**

### Column Name Matching

The `[GSheetColumn]` name parameter is **optional**. When omitted, the property name is used:

```csharp
// These are equivalent:
[property: GSheetColumn("CoinsAmount")] decimal CoinsAmount
[property: GSheetColumn]               decimal CoinsAmount
```

Column matching is **naming-convention-agnostic** — all of these sheet headers match a `CoinsAmount` property:

| Sheet Header | Convention |
|---|---|
| `CoinsAmount` | PascalCase |
| `coinsAmount` | camelCase |
| `coins_amount` | snake_case |
| `coinsamount` | flatcase |
| `COINS_AMOUNT` | UPPER_SNAKE |
| `coins-amount` | kebab-case |
| `Coins Amount` | Title Case |

Underscores, hyphens, and spaces are stripped, then compared case-insensitively.

---

## Tier 2 — Attribute + Config

When you need custom validation rules, ordering, or transforms, add a config class. The domain type is still the row type:

```csharp
[GSheetEntity("ItemPrices", "A1:E")]
public record ItemPriceEntity(
    [property: GSheetColumn("ItemId"), GSheetRequired,
     GSheetRef(typeof(ItemEntity), nameof(ItemEntity.ItemId))]
    string ItemId,

    [property: GSheetColumn("Currency"), GSheetRequired,
     GSheetRef(typeof(CurrencyEntity), nameof(CurrencyEntity.Code))]
    string Currency,

    [property: GSheetColumn("Price"), GSheetRequired, GSheetRange(0.01, 999_999)]
    decimal Price,

    [property: GSheetColumn("DiscountPrice")]
    decimal? DiscountPrice,

    [property: GSheetColumn("IsActive")]
    bool IsActive);

public class ItemPriceEntityConfig : GSheetEntityConfig<ItemPriceEntity>
{
    public override void Configure(GSheetEntityBuilder<ItemPriceEntity> b)
    {
        b.OrderBy(x => x.ItemId).ThenBy(x => x.Currency);

        b.Rule(x => x.DiscountPrice)
            .Must((entity, dp) => dp is null || dp < entity.Price)
            .WithMessage("Discount price must be less than the regular price");
    }
}
```

**Discovery:** The framework finds `ItemPriceEntityConfig`, sees it targets `ItemPriceEntity`, and reads the `[GSheetEntity]` attribute from that type for sheet/range info.

---

## Tier 3 — Full Config with Relationships

When the sheet row structure differs from the domain model — e.g., aggregation, HasMany relationships, or separate row/domain types:

```csharp
// Row type — parsed from the sheet
public record TournamentEntityRow(
    [property: GSheetColumn("TournamentId"), GSheetRequired] string TournamentId,
    [property: GSheetColumn("Name"), GSheetRequired] string Name,
    [property: GSheetColumn("EntryCurrency"), GSheetRequired,
     GSheetRef(typeof(CurrencyEntity), nameof(CurrencyEntity.Code))]
    string EntryCurrency,
    [property: GSheetColumn("EntryFee"), GSheetRequired, GSheetRange(0, 999_999)] decimal EntryFee,
    [property: GSheetColumn("Status"), GSheetRequired] string Status);

// Domain type — enriched with child data
public record TournamentEntity(
    string TournamentId,
    string Name,
    string EntryCurrency,
    decimal EntryFee,
    string Status,
    IReadOnlyList<TournamentRewardEntity> RewardTiers);

public class TournamentEntityConfig : GSheetEntityConfig<TournamentEntityRow, TournamentEntity>
{
    public override void Configure(GSheetEntityBuilder<TournamentEntityRow, TournamentEntity> b)
    {
        b.Sheet("Tournaments", "A1:E");
        b.HasKey(x => x.TournamentId);

        b.Rule(x => x.Status)
            .Must(s => new[] { "Active", "Paused", "Completed" }.Contains(s))
            .WithMessage(s => $"Invalid status '{s}'");

        b.HasMany<TournamentRewardEntity>()
            .WithForeignKey(r => r.TournamentId)
            .Ordered(r => r.PlaceFrom);

        b.OrderBy(x => x.TournamentId);

        b.Map((row, ctx) => new TournamentEntity(
            row.TournamentId,
            row.Name,
            row.EntryCurrency,
            row.EntryFee,
            row.Status,
            ctx.Children<TournamentRewardEntity>(row.TournamentId)));
    }
}
```

**Key differences from Tier 2:**
- Inherit `GSheetEntityConfig<TRow, TDomain>` (two type parameters)
- Call `Sheet()` to set sheet name/range (since `[GSheetEntity]` may not be on the row type)
- Use `Map()` for context-aware mapping (accesses children, resolved upstream data)
- Use `HasMany<TChild>()` for parent-child relationships

---

## Fluent Builder API

The `GSheetEntityBuilder<TRow, TDomain>` provides:

### Sheet Binding

```csharp
b.Sheet("SheetName");              // Range is optional — omit to read all columns
b.Sheet("SheetName", "A1:Z");     // Or specify a range to limit columns
b.HasKey(x => x.Id);              // Declare identity key
```

### Validation — Property Rules

FluentValidation-style per-property rules:

```csharp
b.Rule(x => x.Price)
    .Must(p => p > 0)
    .WithMessage("Price must be positive");

b.Rule(x => x.MaxPlayers)
    .Must((row, max) => max >= row.MinPlayers)    // Row-context predicate
    .WithMessage(max => $"Max ({max}) must be >= Min");  // Dynamic message
```

### Validation — Inline

Lambda validators for complex multi-row checks:

```csharp
b.Validate(rows => rows
    .GroupBy(r => r.Code)
    .Where(g => g.Count() > 1)
    .Select(g => $"Duplicate code: {g.Key}"));
```

### Validation — Graph

Cross-sheet validation that runs after upstream data is available:

```csharp
b.ValidateGraph((rows, ctx) =>
{
    var ids = rows.Select(r => r.TournamentId).ToHashSet();
    var rewards = ctx.GetOptionalDomain<TournamentRewardEntity>();
    return rewards
        .Where(r => !ids.Contains(r.TournamentId))
        .Select(r => $"Orphan reward for tournament '{r.TournamentId}'");
});
```

### Ordering

```csharp
b.OrderBy(x => x.Name);                    // Ascending
b.OrderBy(x => x.Category).ThenBy(x => x.Name);  // Compound
b.OrderByDesc(x => x.CreatedAt);           // Descending
```

### Dependencies

```csharp
b.DependsOn<CurrencyEntity>();   // Explicit dependency (also inferred from [GSheetRef])
```

### Transform

```csharp
b.Transform(rows => rows.Where(r => r.IsActive).ToList());
b.Transform((rows, ctx) => rows.OrderBy(r => r.Id).ToList());
```

### Relationships

```csharp
b.HasMany<RewardEntity>()
    .WithForeignKey(r => r.TournamentId)
    .Ordered(r => r.PlaceFrom);
```

Children are pre-grouped by foreign key and available in `Map()` via `ctx.Children<T>(key)`.

### Mapping

```csharp
// Simple (no context)
b.MapSimple(row => new Domain(row.Id, row.Name));

// Context-aware (access children, upstream data)
b.Map((row, ctx) => new Domain(
    row.Id,
    row.Name,
    ctx.Children<Reward>(row.Id)));
```

### Aggregation

For advanced cases, provide a custom `IAggregator<TRow, TDomain>`:

```csharp
b.UseAggregator(new MyCustomAggregator());
```

---

## EntityMappingContext

Available in `Map()` lambdas for Tier 3 entities:

| Method | Description |
|---|---|
| `Children<T>(key)` | Gets children matching the foreign key value |
| `Resolve<T>(key)` | Resolves a single upstream domain entity |
| `ResolveRequired<T>()` | Gets all upstream data (throws if missing) |
| `GetDomain<T>()` | Gets all domain data of a type from the import context |

---

## Registration

```csharp
// Discover all entities in an assembly
builder.Services.AddGSheetEntitiesInAssemblyOf<CurrencyEntity>();

// Can combine with pipeline/importer registration
builder.Services.AddGSheetImportersInAssemblyOf<SomeImporter>();
```

Both entity adapters and traditional importers/pipelines participate in the same orchestrator — dependency ordering, batch preloading, and the execution plan all work across API boundaries.

---

## How It Works

Entity registration (`AddGSheetEntitiesInAssemblyOf<T>()`) eagerly:

1. Scans the assembly for `[GSheetEntity]` types and `GSheetEntityConfig<>` subclasses
2. Creates `GSheetEntityBuilder` instances and runs `Configure()` for Tier 2/3
3. Compiles each builder into an `EntityPipelineAdapter<TRow, TDomain>` that implements `IGSheetPipeline<TRow, TDomain>`
4. Registers the adapter in DI and in the static entity registry

At import time, the orchestrator treats entity adapters exactly like pipelines — same execution plan, same stages, same dependency ordering. The adapter bridges the entity configuration to the pipeline engine:

| Entity Feature | Pipeline Stage |
|---|---|
| Schema attributes | Structural + attribute validation |
| `Rule()` / `Validate()` | `IRowValidator<TRow>` |
| `ValidateGraph()` | `IGraphValidator<TRow>` |
| `OrderBy()` / `Transform()` | `IRowTransformer<TRow>` |
| `Map()` / `MapSimple()` | `MapToDomain()` |
| `HasMany()` | `IAggregator<TRow, TDomain>` with pre-grouped children |
| `[GSheetRef]` | Reference resolution (same as Pipeline API) |

---

## Comparison: Pipeline vs Entity

A simple currency config takes:

**Pipeline API** (26 lines):
```csharp
public class CurrencyRow { /* 3 properties + attributes */ }
public record CurrencyConfig(string Code, string Name, int DecimalPlaces);

[GSheetImporter("Currencies", "A1:C")]
public class CurrencyPipeline : SimpleGSheetImporter<CurrencyRow, CurrencyConfig>
{
    protected override CurrencyConfig Map(CurrencyRow row) =>
        new(row.Code, row.Name, row.DecimalPlaces);
}
```

**Entity API** (5 lines):
```csharp
[GSheetEntity("Currencies", "A1:C")]
public record CurrencyEntity(
    [property: GSheetColumn, GSheetRowKey, GSheetRequired] string Code,
    [property: GSheetColumn, GSheetRequired] string Name,
    [property: GSheetColumn, GSheetRange(0, 8)] int DecimalPlaces);
```

A complex tournament with parent-child relationships, validation, and graph checks:

**Pipeline API**: ~100 lines across 4 files (Row, Pipeline, Aggregator, 2 Validators)

**Entity API**: ~60 lines in 2 files (Entity record + Config class)

---

## When to Use Each API

| Scenario | Recommended API |
|---|---|
| Simple 1:1 sheet → model | **Entity Tier 1** |
| Need custom validation rules | **Entity Tier 2** |
| Parent-child relationships | **Entity Tier 3** |
| Complex multi-row aggregation | **Entity Tier 3** or Pipeline |
| Need custom `ClassMap` | Pipeline |
| Need full `ProcessAsync` control | Pipeline |
| Custom pipeline observers | Both (observers work with entities too) |
