using CoreLibs.LiveConfig.Examples.Worker.Domain;
using CoreLibs.LiveConfig.GSheet;
using CoreLibs.LiveConfig.GSheet.Pipeline;
using CoreLibs.LiveConfig.GSheet.Schema;

namespace CoreLibs.LiveConfig.Examples.Worker.Sources.GSheet.Pipeline;

// ── Row DTO (bundles reference Items + Currencies) ───────────

public record ItemBundleRow(
    [property: GSheetColumn("BundleId")]
    [property: GSheetRequired]
    string BundleId,
    [property: GSheetColumn("Name")]
    [property: GSheetRequired]
    string Name,
    [property: GSheetColumn("ItemId")]
    [property: GSheetRequired]
    [property: GSheetRef(typeof(ItemConfig), nameof(ItemConfig.ItemId))]
    string ItemId,
    [property: GSheetColumn("Currency")]
    [property: GSheetRequired]
    [property: GSheetRef(typeof(CurrencyConfig), nameof(CurrencyConfig.Code))]
    string Currency,
    [property: GSheetColumn("BundlePrice")]
    [property: GSheetRequired]
    [property: GSheetRange(0.01, 999_999)]
    decimal BundlePrice);

// ── Pipeline (aggregation: multiple rows → single bundle) ────

[GSheetImporter("ItemBundles", "A1:E", Needs = [typeof(ItemImporter), typeof(CurrencyImporter)])]
public sealed class ItemBundleImporter : IGSheetPipeline<ItemBundleRow, ItemBundleConfig>
{
    public IAggregator<ItemBundleRow, ItemBundleConfig> Aggregator => new ItemBundleAggregator();
}

// ── Aggregator: N rows per bundle → 1 ItemBundleConfig ───────

public sealed class ItemBundleAggregator : IAggregator<ItemBundleRow, ItemBundleConfig>
{
    public IEnumerable<ItemBundleConfig> Aggregate(
        IReadOnlyList<ItemBundleRow> rows,
        GSheetImportContext context)
    {
        return rows
            .GroupBy(r => r.BundleId, StringComparer.Ordinal)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select(g =>
            {
                var first = g.First();
                return new ItemBundleConfig(
                    first.BundleId,
                    first.Name,
                    g.Select(r => r.ItemId).Distinct(StringComparer.Ordinal).OrderBy(id => id).ToList(),
                    first.Currency,
                    first.BundlePrice);
            });
    }
}
