using CoreLibs.LiveConfig.GSheet.Entity;
using CoreLibs.LiveConfig.GSheet.Schema;

namespace CoreLibs.LiveConfig.Examples.Worker.Sources.GSheet.EntityApi;

// ═══════════════════════════════════════════════════════════════
// Tier 1 — Pure Attribute Entity (no config class)
// Compare with: Sources/GSheet/Pipeline/ItemImporter.cs (14 lines of Row + 13 lines of Pipeline)
// ═══════════════════════════════════════════════════════════════

[GSheetEntity("Items", "A1:D")]
public record ItemEntity(
    [property: GSheetColumn("ItemId")]
    [property: GSheetRowKey]
    [property: GSheetRequired]
    string ItemId,
    [property: GSheetColumn("Name")]
    [property: GSheetRequired]
    string Name,
    [property: GSheetColumn("Rarity")]
    [property: GSheetRequired]
    string Rarity,
    [property: GSheetColumn("Category")]
    [property: GSheetRequired]
    string Category);
