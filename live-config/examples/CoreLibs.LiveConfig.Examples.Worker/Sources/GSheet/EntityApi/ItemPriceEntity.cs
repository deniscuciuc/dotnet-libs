using CoreLibs.LiveConfig.GSheet.Entity;
using CoreLibs.LiveConfig.GSheet.Schema;

namespace CoreLibs.LiveConfig.Examples.Worker.Sources.GSheet.EntityApi;

// ═══════════════════════════════════════════════════════════════
// Tier 2 — Attribute + Config (custom validation + ordering)
// Compare with: Sources/GSheet/Pipeline/ItemPriceImporter.cs (26 lines of Row + Pipeline + Validator)
// ═══════════════════════════════════════════════════════════════

[GSheetEntity("ItemPrices", "A1:E")]
public record ItemPriceEntity(
    [property: GSheetColumn("ItemId")]
    [property: GSheetRequired]
    [property: GSheetRef(typeof(ItemEntity), nameof(ItemEntity.ItemId))]
    string ItemId,
    [property: GSheetColumn("Currency")]
    [property: GSheetRequired]
    [property: GSheetRef(typeof(CurrencyEntity), nameof(CurrencyEntity.Code))]
    string Currency,
    [property: GSheetColumn("Price")]
    [property: GSheetRequired]
    [property: GSheetRange(0.01, 999_999)]
    decimal Price,
    [property: GSheetColumn("DiscountPrice")]
    decimal? DiscountPrice,
    [property: GSheetColumn("IsActive")] bool IsActive);

/// <summary>
/// Tier 2 config: adds fluent validation rules + ordering.
/// No separate Row type needed — the domain IS the row.
/// </summary>
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
