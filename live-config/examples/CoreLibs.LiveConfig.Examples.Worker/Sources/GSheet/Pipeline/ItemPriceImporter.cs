using CoreLibs.LiveConfig.Examples.Worker.Domain;
using CoreLibs.LiveConfig.GSheet;
using CoreLibs.LiveConfig.GSheet.Pipeline;
using CoreLibs.LiveConfig.GSheet.Schema;

namespace CoreLibs.LiveConfig.Examples.Worker.Sources.GSheet.Pipeline;

// ── Row DTO (cross-cutting: references Items + Currencies) ───

public record ItemPriceRow(
    [property: GSheetColumn("ItemId")]
    [property: GSheetRequired]
    [property: GSheetRef(typeof(ItemConfig), nameof(ItemConfig.ItemId))]
    string ItemId,
    [property: GSheetColumn("Currency")]
    [property: GSheetRequired]
    [property: GSheetRef(typeof(CurrencyConfig), nameof(CurrencyConfig.Code))]
    string Currency,
    [property: GSheetColumn("Price")]
    [property: GSheetRequired]
    [property: GSheetRange(0.01, 999_999)]
    decimal Price,
    [property: GSheetColumn("DiscountPrice")]
    decimal? DiscountPrice,
    [property: GSheetColumn("IsActive")] bool IsActive);

// ── Pipeline (Needs Items + Currencies) ──────────────────────

[GSheetImporter("ItemPrices", "A1:E", Needs = [typeof(ItemImporter), typeof(CurrencyImporter)])]
public sealed class ItemPriceImporter : IGSheetPipeline<ItemPriceRow, ItemPriceConfig>
{
    public IRowValidator<ItemPriceRow> RowValidator => new ItemPriceValidator();

    public IEnumerable<ItemPriceConfig> MapToDomain(
        IReadOnlyList<ItemPriceRow> rows,
        GSheetImportContext context)
    {
        return rows
            .OrderBy(r => r.ItemId, StringComparer.Ordinal)
            .ThenBy(r => r.Currency, StringComparer.Ordinal)
            .Select(r => new ItemPriceConfig(r.ItemId, r.Currency, r.Price, r.DiscountPrice, r.IsActive));
    }
}

// ── Custom row validator ─────────────────────────────────────

public sealed class ItemPriceValidator : IRowValidator<ItemPriceRow>
{
    public GSheetValidationResult Validate(IReadOnlyList<ItemPriceRow> rows, GSheetImportContext context)
    {
        var errors = new List<GSheetValidationError>();

        for (var i = 0; i < rows.Count; i++)
            if (rows[i].DiscountPrice is > 0 && rows[i].DiscountPrice >= rows[i].Price)
                errors.Add(new GSheetValidationError(i, nameof(ItemPriceRow.DiscountPrice),
                    "Discount price must be less than the regular price"));

        return errors.Count == 0 ? GSheetValidationResult.Ok() : GSheetValidationResult.Fail(errors);
    }
}
