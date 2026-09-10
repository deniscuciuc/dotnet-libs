# Google Sheets Integration

The `CoreLibs.LiveConfig.GSheet` package provides a full-featured Google Sheets config source with CSV parsing, typed importers, a declarative pipeline system, dependency ordering, validation, and batch-optimized API calls.

## Setup

```csharp
// Register GSheet source + core services
builder.Services.AddLiveConfigGSheet(builder.Configuration);

// Auto-discover all importers/pipelines in an assembly
builder.Services.AddGSheetImportersInAssemblyOf<BonusesImporter>();


// Auto-discover all entities in an assembly (see Entity API docs)
builder.Services.AddGSheetEntitiesInAssemblyOf<CurrencyEntity>();

// Or register specific importers
builder.Services.AddGSheetImporter<BonusesImporter>();
builder.Services.AddGSheetImporter<QuestPipeline>();
```

## Configuration

```yaml
GSheet:
  spreadsheetId: "1ABC123_your_spreadsheet_id"
  applicationName: "LiveConfigManager"
  failOnImportErrors: true

  failOnValidationErrors: false
  importMode: SkipInvalid       # Strict | SkipInvalid | CollectAndReport
  dryRun: false                 # Preview mode — runs pipeline without side effects

  credentials:
    method: CredentialsBase64Env
    value: GOOGLE_SHEET_CREDENTIALS
```

| Option | Default | Description |
| --- | --- | --- |
| `SpreadsheetId` | — | Google Sheets spreadsheet ID (required) |
| `ApplicationName` | `"LiveConfig"` | Google API application name |
| `FailOnImportErrors` | `true` | Throw on import failures |
| `FailOnValidationErrors` | `false` | Throw on validation errors |
| `ImportMode` | `SkipInvalid` | How to handle invalid rows: `Strict` (throw), `SkipInvalid` (filter), `CollectAndReport` (continue) |
| `DryRun` | `false` | If true, runs the full pipeline without persisting results |
| `Credentials.Method` | `CredentialsBase64Env` | Credential resolution method |
| `Credentials.Value` | — | Depends on method: env var name, base64 string, or file path |

### Credential Methods

| Method | Value Contains |
| --- | --- |
| `CredentialsBase64Env` | Name of env var holding Base64-encoded service account JSON |

| `Base64JsonFromConfiguration` | Base64-encoded service account JSON directly |
| `Path` | File path to service account JSON |

---

## Three APIs

The GSheet layer supports three APIs side-by-side. All are fully supported by the orchestrator:

| | `IGSheetImporter<TRow, TDomain>` | `IGSheetPipeline<TRow, TDomain>` | Entity API |
| --- | --- | --- | --- |
| ClassMap | Required | Optional (auto-maps from attributes) | Automatic |
| Validation | Manual `ValidateRows()` | Automatic from attributes + optional custom | Automatic from attributes + fluent rules |
| Aggregation | None (1:1 row→domain) | Built-in `IAggregator<TRow, TDomain>` | HasMany + Map() |

| Transformation | None | `IRowTransformer<TRow>` | OrderBy + Transform() |
| Cross-sheet refs | Manual | Auto via `[GSheetRef]` | Auto via `[GSheetRef]` |
| Error handling | Boolean return | `GSheetPipelineResult<TDomain>` | Full diagnostics |
| Config style | Implement interface | Override Configure() | Attributes + optional config class |

> **New to GSheet?** Start with the [Entity API](gsheet-entity.md) — it's the simplest path.
> Use `IGSheetPipeline` when you need full control over pipeline stages.

---

## Pipeline API (Recommended)

### 1. Define Row + Domain with Attributes

```csharp
public class BonusRow
{
    [GSheetColumn("Name")]
    [GSheetRequired]
    public string Name { get; set; } = "";

    [GSheetColumn("Amount")]
    [GSheetRange(0.01, 10000)]

    public decimal Amount { get; set; }

    [GSheetColumn("Currency")]
    [GSheetRef(typeof(CurrencyConfig), nameof(CurrencyConfig.Code))]
    public string Currency { get; set; } = "";

    [GSheetColumn("MinLevel")]
    [GSheetRange(1, 100)]
    public int MinLevel { get; set; }
}

public record BonusConfig(string Name, decimal Amount, string Currency, int MinLevel);
```

### 2. Implement `IGSheetPipeline<TRow, TDomain>`

#### Minimal (zero-code validation)

```csharp
[GSheetImporter("Bonuses", "A1:D")]
public class BonusesPipeline : SimpleGSheetImporter<BonusRow, BonusConfig>
{
    protected override BonusConfig Map(BonusRow row) =>
        new(row.Name, row.Amount, row.Currency, row.MinLevel);
}
```

`SimpleGSheetImporter` auto-maps from attributes, runs structural + attribute validation, and handles the 1:1 row→domain case.

#### Full pipeline with all stages

```csharp
[GSheetImporter("Quests", "A1:H", Needs = [typeof(CurrencyPipeline)])]
public class QuestPipeline : GSheetPipeline<QuestRow, QuestConfig>
{
    protected override void Configure()
    {
        UseValidator<QuestRowValidator>();
        UseGraphValidator<QuestGraphValidator>();
        UseTransformer<QuestRowTransformer>();
        UseAggregator<QuestAggregator>();
    }

    public override Task<GSheetPipelineResult<QuestConfig>> ProcessAsync(
        IReadOnlyList<QuestConfig> domains,
        GSheetImportContext context)
    {
        // Persist or further process
        return Task.FromResult(GSheetPipelineResult<QuestConfig>.Success(
            domains, domains.Count, domains.Count));
    }
}
```

### Schema Attributes

| Attribute | Target | Description |
| --- | --- | --- |
| `[GSheetColumn("Name")]` | Property | Maps to column header. Name is optional — defaults to property name. Optional `Order` for positional. |
| `[GSheetIgnore]` | Property | Excludes from mapping |
| `[GSheetDefault(value)]` | Property | Fallback for empty cells |
| `[GSheetRequired]` | Property | Fails if null/whitespace/default |
| `[GSheetRange(min, max)]` | Property | Numeric bounds validation |
| `[GSheetRegex(pattern)]` | Property | Regex pattern validation |
| `[GSheetRowKey]` | Property | Identity property for aggregation/dedup |
| `[GSheetRef(type, key)]` | Property | Cross-sheet reference validation |

> **Flexible header matching:** Column names are matched across naming conventions. A property named `CoinsAmount` matches headers `coins_amount`, `coinsAmount`, `coinsamount`, `COINS_AMOUNT`, `coins-amount`, etc. Underscores, hyphens, and spaces are stripped before case-insensitive comparison.

### Pipeline Stages

The orchestrator runs these stages in order:

1. **Structural validation** — checks sheet headers against `[GSheetColumn]` attributes
2. **Parse** — CsvHelper with auto-mapped or custom `ClassMap`
3. **Attribute validation** — `[GSheetRequired]`, `[GSheetRange]`, `[GSheetRegex]`
4. **Custom row validation** — `IRowValidator<TRow>` (optional)
5. **Row filtering** — invalid rows filtered per `ImportMode`
6. **Transformation** — `IRowTransformer<TRow>` (optional)
7. **Graph validation** — `IGraphValidator<TRow>` (optional: cross-row rules)
8. **Reference resolution** — `[GSheetRef]` auto-validated against upstream domains
9. **Aggregation / MapToDomain** — `IAggregator<TRow, TDomain>` or `MapToDomain()`
10. **Store in context** — domain data available to downstream importers
11. **ProcessAsync** — final persistence/side effects

### Reference System

Cross-sheet references are validated automatically:

```csharp
[GSheetColumn("Currency")]
[GSheetRef(typeof(CurrencyConfig), nameof(CurrencyConfig.Code))]
public string Currency { get; set; } = "";
```

If the `CurrencyConfig` domain doesn't contain a matching `Code`, the pipeline produces a clear error:

```text
Reference 'USDX' not found in CurrencyConfig.Code (sheet 'Rewards', row 12)
```

Dependencies are automatically inferred from `[GSheetRef]` annotations.

### Import Modes

| Mode | Behavior |
| --- | --- |
| `Strict` | Throws immediately on any validation error |
| `SkipInvalid` | Filters invalid rows, continues with valid ones (default) |
| `CollectAndReport` | Collects all errors, continues processing, reports at end |

### Import Report

After a full import run, the `GSheetImportContext.Report` contains per-importer diagnostics:

```csharp
var report = context.Report;
Console.WriteLine($"Total: {report.TotalRows} rows, {report.TotalValidRows} valid");
Console.WriteLine($"Errors: {report.TotalErrors}, Warnings: {report.TotalWarnings}");

foreach (var ir in report.ImporterReports)
{
    Console.WriteLine($"  {ir.ImporterName}: {ir.ValidRows}/{ir.TotalRows} rows in {ir.Duration.TotalMilliseconds}ms");
}
```

---

## Legacy API

The original `IGSheetImporter<TRow, TDomain>` API remains fully supported:

```csharp
[GSheetImporter("Bonuses", "A1:D")]
public class BonusesImporter : IGSheetImporter<BonusRow, BonusConfig>
{
    public ClassMap<BonusRow> CreateMapper() => new BonusRowMap();

    public GSheetValidationResult ValidateRows(IReadOnlyList<BonusRow> rows)
    {
        var errors = new List<GSheetValidationError>();
        for (var i = 0; i < rows.Count; i++)
        {
            if (rows[i].Amount <= 0)
                errors.Add(new GSheetValidationError(i, "Amount", "Must be positive"));
        }
        return errors.Count == 0 ? GSheetValidationResult.Ok() : GSheetValidationResult.Fail(errors);
    }

    public IEnumerable<BonusConfig> MapToDomain(IReadOnlyList<BonusRow> rows, GSheetImportContext context)
        => rows.Select(r => new BonusConfig(r.Name, r.Amount, r.Currency, r.MinLevel));

    public Task<bool> ImportAsync(IEnumerable<BonusConfig> domains, GSheetImportContext context)
        => Task.FromResult(true);
}
```

See [Migration Guide](gsheet-migration.md) for upgrading from `IGSheetImporter` to `IGSheetPipeline`.

---

## Execution Model

### Batch Preloading

The orchestrator collects all `(sheetName, range)` pairs from registered importers and fetches them in a **single batch API call** to Google Sheets.

### Dependency-Ordered Execution

Importers are topologically sorted by their `Needs` dependencies and executed in parallel within each tier:

```text
Tier 0: CurrencyPipeline, LanguagePipeline       ← parallel
Tier 1: BonusesPipeline, TranslationsPipeline     ← parallel (after tier 0)
Tier 2: CompositeBonusPipeline                    ← after tier 1
```

### Import Context

The `GSheetImportContext` is shared across all importers. Downstream importers can read upstream data:

```csharp
var currencies = context.GetDomain<CurrencyConfig>();
```

---

## CsvHelper Converters

### Built-in Converters

| Converter | Description |
| --- | --- |
| `EnumConverter<T>` | Case-insensitive enum parsing |
| `IntListConverter` | `"1, 2, 3"` → `List<int>` |
| `DecimalListConverter` | `"1.5, 2.3"` → `List<decimal>` |
| `DoubleListConverter` | `"1.5, 2.3"` → `List<double>` |
| `StringListConverter<TSep>` | String lists with configurable separator |
| `DayOfWeekConverter` | `"Monday"` → `DayOfWeek.Monday` |
| `TimeOnlyConverter` | `"14:30"` → `TimeOnly` |
| `DateOnlyConverter` | `"2025-01-15"` → `DateOnly` |
| `GuidConverter` | GUID string → `Guid` |
| `BoolFlexConverter` | `"yes"/"no"/"1"/"0"/"on"/"off"` → `bool` |
| `DictionaryConverter` | `"key1:val1,key2:val2"` → `Dictionary<string,string>` |
| `RangeConverter` | `"1-10"` → `NumericRange(1, 10)` |
| `WeightedListConverter` | `"gold:50,xp:30"` → `List<WeightedItem<string>>` |
| `JsonCellConverter<T>` | JSON string in cell → `T` |
| `NullableConverter<T>` | Wraps any converter for `T?`, empty → null |

### Separator Types

| Separator | Splits On |
| --- | --- |
| `CommaSeparator` | `,` |
| `WhitespaceSeparator` | Space |
| `CommaAndWhitespaceSeparator` | comma + space |

Usage:

```csharp
Map(m => m.Tags).TypeConverter<StringListConverter<CommaSeparator>>();
Map(m => m.Rewards).TypeConverter<WeightedListConverter>();
Map(m => m.Metadata).TypeConverter<JsonCellConverter<Dictionary<string, object>>>();
```

---

## Deterministic Output

The `CanonicalJsonSerializer` produces deterministic JSON with sorted property names. This ensures identical domain data always produces the same hash, making change detection reliable. Used internally by `GSheetConfigSource`.

---

## Error Handling

### GSheetImportException

Thrown when an individual importer fails. Contains `ImporterName`, `SheetName`, `Range`, `Stage`, and `ValidationErrors`.

### GSheetPreloadException

Thrown when the batch Google Sheets API call fails.

### GSheetImportBatchException

Thrown when multiple importers fail within a parallel batch.

---

## v2 Features

### Optional Column Names

`[GSheetColumn]` can be used without a name — the property name is used as the column header:

```csharp
public record CurrencyRow(
    [property: GSheetColumn, GSheetRequired] string Code,       // column: "Code"
    [property: GSheetColumn, GSheetRequired] string Name,       // column: "Name"
    [property: GSheetColumn, GSheetRange(0, 8)] int DecimalPlaces);
```

You can still pass an explicit name for columns that differ from the property name:

```csharp
[property: GSheetColumn("CurrencyCode")] string Code
```

### Enhanced Context API

The `GSheetImportContext` now provides intent-based accessors:

```csharp
// Throws if upstream data is missing (mandatory dependency)
var currencies = context.GetRequiredDomain<CurrencyConfig>();

// Returns empty list if not available (optional dependency)
var rewards = context.GetOptionalDomain<TournamentRewardTier>();

// Group by key (e.g., rewards by tournament)
var grouped = context.GetGroupedDomain<TournamentRewardTier, string>(r => r.TournamentId);

// Index by unique key (e.g., currencies by code)
var indexed = context.GetIndexedDomain<CurrencyConfig, string>(c => c.Code);

// Resolve a single entity by key (case-insensitive)
var usd = context.Resolve<CurrencyConfig>("USD", c => c.Code);
var usdRequired = context.ResolveRequired<CurrencyConfig>("USD", c => c.Code);
```

### Enhanced Validation Errors

`GSheetValidationError` now carries optional sheet location data:

```csharp
// Factory auto-computes spreadsheet row (rowIndex + 2 for header offset)
var error = GSheetValidationError.ForSheet("Currencies", rowIndex: 3, "Rate", "out of range");
// error.SpreadsheetRow == 5, error.SheetName == "Currencies"

// ToString() includes location: "[Currencies!Row 5] Rate - out of range"
```

Validators automatically receive the sheet name via `context.SheetName`.

### Composite Validators

Multiple validators can be chained via `UseValidator`/`UseGraphValidator` — calling them multiple times appends to a composite:

```csharp
[GSheetImporter("Tournaments", "A1:H")]
public class TournamentPipeline : GSheetPipeline<TournamentRow, TournamentConfig>
{
    protected override void Configure()
    {
        UseValidator<StatusValidator>();
        UseValidator<PlayerCountValidator>();  // both run, errors merged
        UseGraphValidator<OrphanDetector>();
        UseGraphValidator<DuplicateIdDetector>();
    }
}
```

Or use `CompositeRowValidator<T>` / `CompositeGraphValidator<T>` directly for programmatic composition.

### Cross-Row and Cross-Sheet Validation

`IGraphValidator<TRow>` runs after row validation and has access to the full import context:

```csharp
// Cross-row: detect overlapping reward ranges per tournament
public class RewardOverlapValidator : IGraphValidator<TournamentRewardRow>
{
    public GSheetValidationResult Validate(IReadOnlyList<TournamentRewardRow> rows, GSheetImportContext context)
    {
        // Group by tournament, check for PlaceFrom/PlaceTo overlaps
    }
}

// Cross-sheet: detect orphan rewards referencing nonexistent tournaments
public class TournamentOrphanValidator : IGraphValidator<TournamentRow>
{
    public GSheetValidationResult Validate(IReadOnlyList<TournamentRow> rows, GSheetImportContext context)
    {
        var tournamentIds = rows.Select(r => r.TournamentId).ToHashSet();
        var rewardRows = context.GetRows<TournamentRewardRow>() ?? [];
        // Flag rewards with unknown tournament IDs
    }
}
```

### Pipeline Lifecycle Observers

Register observers to hook into pipeline stage transitions:

```csharp
// Register in DI
builder.Services.AddGSheetPipelineObserver<LoggingPipelineObserver>();

// Or create custom observers
public class MetricsObserver : IGSheetPipelineObserver
{
    public void OnAfterParse(string pipelineName, int rowCount) { /* record metric */ }
    public void OnCompleted(string pipelineName, TimeSpan elapsed, bool success) { /* record duration */ }
    // All other methods have default no-op implementations
}
```

Observer hooks: `OnBeforeParse`, `OnAfterParse`, `OnBeforeValidation`, `OnAfterValidation`, `OnBeforeMapping`, `OnAfterMapping`, `OnCompleted`.

### Deterministic Ordering

All example importers use `OrderBy` for deterministic domain output. This ensures config hashes are stable across imports:

```csharp
return rows
    .OrderBy(r => r.Code, StringComparer.Ordinal)
    .Select(r => new CurrencyConfig(r.Code, r.Name, r.DecimalPlaces));
```
