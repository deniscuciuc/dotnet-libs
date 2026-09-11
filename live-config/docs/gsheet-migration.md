# GSheet Migration Guide: IGSheetImporter → IGSheetPipeline

This guide covers migrating from the original `IGSheetImporter<TRow, TDomain>` API to the new `IGSheetPipeline<TRow, TDomain>` API.

> **The legacy API is not deprecated.** Both APIs are fully supported by the orchestrator. Migrate at your own pace — you can mix pipeline and legacy importers in the same project.

## Quick Comparison

| Before (Legacy) | After (Pipeline) |
| --- | --- |
| `ClassMap<TRow>` required | Auto-map from `[GSheetColumn]` attributes |
| Manual `ValidateRows()` | Automatic from `[GSheetRequired]`, `[GSheetRange]`, `[GSheetRegex]` |
| `MapToDomain()` returns `IEnumerable<TDomain>` | `MapToDomain()` or `IAggregator` for N:1 |
| `ImportAsync()` returns `bool` | `ProcessAsync()` returns `GSheetPipelineResult<TDomain>` |
| No cross-sheet reference validation | `[GSheetRef]` with auto-validation |

## Step-by-Step Migration

### Step 1: Add Schema Attributes

Replace your row type with attribute-annotated properties:

**Before:**

```csharp
public record BonusRow(string Name, decimal Amount, string Currency, int MinLevel);
```

**After:**

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
    public string Currency { get; set; } = "";

    [GSheetColumn("MinLevel")]
    [GSheetRange(1, 100)]
    public int MinLevel { get; set; }
}
```

> **Tip:** If you don't add any `[GSheetColumn]` attributes, all public readable properties are auto-mapped by their property name (convention-based).

### Step 2: Choose Your Base Class

#### For 1:1 row → domain mappings

Use `SimpleGSheetImporter<TRow, TDomain>`:

```csharp
[GSheetImporter("Bonuses", "A1:D")]
public class BonusesPipeline : SimpleGSheetImporter<BonusRow, BonusConfig>
{
    protected override BonusConfig Map(BonusRow row) =>
        new(row.Name, row.Amount, row.Currency, row.MinLevel);
}
```

This replaces the entire `IGSheetImporter` implementation — no ClassMap, no ValidateRows, no ImportAsync.

#### For complex pipelines (aggregation, transformation, graph validation)

Use `GSheetPipeline<TRow, TDomain>`:

```csharp
[GSheetImporter("Quests", "A1:H")]
public class QuestPipeline : GSheetPipeline<QuestRow, QuestConfig>
{
    protected override void Configure()
    {
        UseValidator<QuestRowValidator>();
        UseAggregator<QuestAggregator>();
    }
}
```

### Step 3: Move Validation Logic

**Before (manual validation):**

```csharp
public GSheetValidationResult ValidateRows(IReadOnlyList<BonusRow> rows)
{
    var errors = new List<GSheetValidationError>();
    for (var i = 0; i < rows.Count; i++)
    {
        if (string.IsNullOrWhiteSpace(rows[i].Name))
            errors.Add(new GSheetValidationError(i, "Name", "Name is required"));
        if (rows[i].Amount <= 0)
            errors.Add(new GSheetValidationError(i, "Amount", "Amount must be positive"));
    }
    return errors.Count == 0 ? GSheetValidationResult.Ok() : GSheetValidationResult.Fail(errors);
}
```

**After:**

- `Name` → `[GSheetRequired]` attribute (automatic)
- `Amount` → `[GSheetRange(0.01, 10000)]` attribute (automatic)
- Custom business rules → `IRowValidator<TRow>`:

```csharp
public class BonusRowValidator : IRowValidator<BonusRow>
{
    public GSheetValidationResult Validate(IReadOnlyList<BonusRow> rows, GSheetImportContext context)
    {
        // Only implement rules that can't be expressed as attributes
        var errors = new List<GSheetValidationError>();
        for (var i = 0; i < rows.Count; i++)
        {
            if (rows[i].Name.Contains("test", StringComparison.OrdinalIgnoreCase))
                errors.Add(new GSheetValidationError(i, "Name", "Test data not allowed"));
        }
        return errors.Count == 0 ? GSheetValidationResult.Ok() : GSheetValidationResult.Fail(errors);
    }
}
```

### Step 4: Replace Cross-Sheet Lookups

**Before (manual lookup):**

```csharp
public GSheetValidationResult ValidateRows(IReadOnlyList<RewardRow> rows)
{
    var currencies = context.GetDomain<CurrencyConfig>()?.ToHashSet(c => c.Code);
    // manual lookup...
}
```

**After:**

```csharp
[GSheetColumn("Currency")]
[GSheetRef(typeof(CurrencyConfig), nameof(CurrencyConfig.Code))]
public string Currency { get; set; } = "";
```

Dependencies are automatically inferred. No manual `Needs` required.

### Step 5: Delete ClassMap

If your ClassMap was straightforward column→property mapping, delete it entirely. The pipeline auto-generates it from `[GSheetColumn]` attributes.

**Before:**

```csharp
public sealed class BonusRowMap : ClassMap<BonusRow>
{
    public BonusRowMap()
    {
        Map(m => m.Name).Name("Name");
        Map(m => m.Amount).Name("Amount");
        Map(m => m.Currency).Name("Currency");
        Map(m => m.MinLevel).Name("MinLevel");
    }
}
```

**After:** Deleted. Attributes handle it.

> **Exception:** If you use custom type converters on specific columns, you still need a ClassMap. Override `CreateMapper()` in your pipeline.

### Step 6: Update DI Registration

No changes needed — `AddGSheetImportersInAssemblyOf<T>()` automatically discovers both `IGSheetImporter` and `IGSheetPipeline` implementations.

## Aggregation (New Feature)

If your existing importer had logic to group multiple rows into one domain entity, you can now use `IAggregator`:

```csharp
public class QuestAggregator : IAggregator<QuestRow, QuestConfig>
{
    public IReadOnlyList<QuestConfig> Aggregate(IReadOnlyList<QuestRow> rows, GSheetImportContext context)
    {
        return rows
            .GroupBy(r => r.QuestId)
            .Select(g => new QuestConfig(
                Id: g.Key,
                Name: g.First().Name,
                Steps: g.Select(r => new QuestStep(r.StepOrder, r.StepDescription)).ToList()))
            .ToList();
    }
}
```

---

## v2 Migration Notes

### Optional Column Names

If your column headers match property names, simplify:

**Before:** `[property: GSheetColumn("Code")]`
**After:** `[property: GSheetColumn]` — uses `"Code"` automatically.

### Enhanced Context API

Replace raw `GetDomain<T>()` calls with intent-based accessors:

| Before | After | Behavior on missing |
| --- | --- | --- |
| `context.GetDomain<T>()` | `context.GetRequiredDomain<T>()` | Throws with clear message |
| `context.GetDomain<T>() ?? []` | `context.GetOptionalDomain<T>()` | Returns empty list |
| Manual grouping | `context.GetGroupedDomain<T, TKey>(selector)` | Returns `ILookup` |
| Manual dictionary | `context.GetIndexedDomain<T, TKey>(selector)` | Returns `IReadOnlyDictionary` |
| Manual lookup | `context.Resolve<T>(key, keySelector)` | Returns `T?`, case-insensitive |

### Composite Validators

`UseValidator<T>()` and `UseGraphValidator<T>()` now **append** instead of replace. Calling them multiple times chains validators into a composite that merges errors.

### Validation Error Location

Validators can now use `GSheetValidationError.ForSheet()` to include sheet name and spreadsheet row number. The orchestrator sets `context.SheetName` automatically.

### Pipeline Observers

Register `IGSheetPipelineObserver` implementations for stage-level hooks:

```csharp
builder.Services.AddGSheetPipelineObserver<LoggingPipelineObserver>();
```
