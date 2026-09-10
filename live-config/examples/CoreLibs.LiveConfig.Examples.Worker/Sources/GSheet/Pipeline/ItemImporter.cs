using CoreLibs.LiveConfig.Examples.Worker.Domain;
using CoreLibs.LiveConfig.GSheet;
using CoreLibs.LiveConfig.GSheet.Pipeline;
using CoreLibs.LiveConfig.GSheet.Schema;

namespace CoreLibs.LiveConfig.Examples.Worker.Sources.GSheet.Pipeline;

// ── Row DTO ───────────────────────────────────────────────────

public record ItemRow(
    [property: GSheetColumn("ItemId")]
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

// ── Pipeline ──────────────────────────────────────────────────

[GSheetImporter("Items", "A1:D")]
public sealed class ItemImporter : IGSheetPipeline<ItemRow, ItemConfig>
{
    public IEnumerable<ItemConfig> MapToDomain(
        IReadOnlyList<ItemRow> rows,
        GSheetImportContext context)
    {
        return rows
            .OrderBy(r => r.ItemId, StringComparer.Ordinal)
            .Select(r => new ItemConfig(r.ItemId, r.Name, r.Rarity, r.Category));
    }
}
