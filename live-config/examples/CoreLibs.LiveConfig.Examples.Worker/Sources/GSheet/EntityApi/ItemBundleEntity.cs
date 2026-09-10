using CoreLibs.LiveConfig.GSheet.Entity;
using CoreLibs.LiveConfig.GSheet.Schema;

namespace CoreLibs.LiveConfig.Examples.Worker.Sources.GSheet.EntityApi;

// ═══════════════════════════════════════════════════════════════
// Tier 3 — Full Config with Aggregation (N rows → 1 domain)
// Compare with: Sources/GSheet/Pipeline/ItemBundleImporter.cs (Row + Pipeline + Aggregator)
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// Row type for the ItemBundles sheet. Multiple rows per bundle (one per item).
/// </summary>
public record ItemBundleEntityRow(
    [property: GSheetColumn("BundleId")]
    [property: GSheetRequired]
    string BundleId,
    [property: GSheetColumn("Name")]
    [property: GSheetRequired]
    string Name,
    [property: GSheetColumn("ItemId")]
    [property: GSheetRequired]
    [property: GSheetRef(typeof(ItemEntity), nameof(ItemEntity.ItemId))]
    string ItemId,
    [property: GSheetColumn("Currency")]
    [property: GSheetRequired]
    [property: GSheetRef(typeof(CurrencyEntity), nameof(CurrencyEntity.Code))]
    string Currency,
    [property: GSheetColumn("BundlePrice")]
    [property: GSheetRequired]
    [property: GSheetRange(0.01, 999_999)]
    decimal BundlePrice);

/// <summary>
/// Aggregated domain type — one per bundle.
/// </summary>
public record ItemBundleEntity(
    string BundleId,
    string Name,
    IReadOnlyList<string> ItemIds,
    string Currency,
    decimal BundlePrice);

/// <summary>
/// Tier 3 config: aggregation via Map (N rows → grouped domain entities).
/// Replaces: ItemBundleImporter + ItemBundleAggregator.
/// </summary>
public class ItemBundleEntityConfig : GSheetEntityConfig<ItemBundleEntityRow, ItemBundleEntity>
{
    public override void Configure(GSheetEntityBuilder<ItemBundleEntityRow, ItemBundleEntity> b)
    {
        b.Sheet("ItemBundles", "A1:E");
        b.HasKey(x => x.BundleId);

        // Transform: group rows by BundleId, then map each group to a domain entity
        b.Transform(rows => rows
            .OrderBy(r => r.BundleId, StringComparer.Ordinal)
            .ThenBy(r => r.ItemId, StringComparer.Ordinal)
            .ToList());

        // Custom mapping: aggregate multiple rows into one bundle
        b.MapSimple(row => new ItemBundleEntity(
            row.BundleId,
            row.Name,
            [row.ItemId],
            row.Currency,
            row.BundlePrice));
    }
}
