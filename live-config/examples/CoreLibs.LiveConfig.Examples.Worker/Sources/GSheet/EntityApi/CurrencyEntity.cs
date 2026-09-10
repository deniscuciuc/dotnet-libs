using CoreLibs.LiveConfig.GSheet.Entity;
using CoreLibs.LiveConfig.GSheet.Schema;

namespace CoreLibs.LiveConfig.Examples.Worker.Sources.GSheet.EntityApi;

// ═══════════════════════════════════════════════════════════════
// Tier 1 — Pure Attribute Entity (no config class)
// Compare with: Sources/GSheet/Pipeline/CurrencyImporter.cs (15 lines of Row + 11 lines of Pipeline)
// ═══════════════════════════════════════════════════════════════

[GSheetEntity("Currencies", "A1:C")]
public record CurrencyEntity(
    [property: GSheetColumn]
    [property: GSheetRowKey]
    [property: GSheetRequired]
    string Code,
    [property: GSheetColumn]
    [property: GSheetRequired]
    string Name,
    [property: GSheetColumn]
    [property: GSheetRange(0, 8)]
    int DecimalPlaces);
